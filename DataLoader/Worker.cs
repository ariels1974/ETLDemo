using Confluent.Kafka;
using MongoDB.Driver;
using Scraping.Statistics;
using System.Text.Json;

namespace DataLoader;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private IConsumer<Null, string>? _consumer;
    private IMongoCollection<ProductScrapingRecord>? _collection;
    private readonly IProducer<Null, string> _producer;
    private readonly ScrapingStatisticsService _statisticsService;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _producer = CreateProducer();
        _statisticsService = new ScrapingStatisticsService(
            configuration["InfluxDB:Url"],
            configuration["InfluxDB:Token"],
            configuration["InfluxDB:Org"],
            configuration["InfluxDB:Bucket"]
        );
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        ConnectMongoDb();
        await base.StartAsync(cancellationToken);
    }

    private void ConnectMongoDb()
    {
        var mongoConn = _configuration["MongoDB:ConnectionString"] ?? "mongodb://root:password@localhost:32017";
        var mongoDb = _configuration["MongoDB:Database"] ?? "ScrapingDb";
        var mongoCollection = _configuration["MongoDB:Collection"] ?? "ProductScrapingRecords";
        var client = new MongoClient(mongoConn);
        var db = client.GetDatabase(mongoDb);
        _collection = db.GetCollection<ProductScrapingRecord>(mongoCollection);
        _logger.LogInformation(
            "MongoDB connected to {ConnectionString}, using database {Database} and collection {Collection}",
            mongoConn, mongoDb, mongoCollection);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CreateKafkaConsumerAndSubscribe();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = _consumer.Consume(stoppingToken);
                try
                {
                    if (_collection == null)
                    {
                        var errorMsg = "MongoDB collection is not initialized.";
                        await CreateDeadLetterMsg(stoppingToken, result, errorMsg);
                        continue;
                    }

                    var record = JsonSerializer.Deserialize<ProductScrapingRecord>(result.Message.Value);
                    if (record != null)
                    {
                        await _statisticsService.WriteScrapingStateAsync(record.SiteName, ScrapingState.Started, "Loader", DateTime.Now);

                        var filter = Builders<ProductScrapingRecord>.Filter.And(
                            Builders<ProductScrapingRecord>.Filter.Eq(x => x.SiteName, record.SiteName),
                            Builders<ProductScrapingRecord>.Filter.Eq(x => x.Description, record.Description),
                            Builders<ProductScrapingRecord>.Filter.Eq(x => x.Price, record.Price)
                        );
                        
                        await _collection.ReplaceOneAsync(filter, record, new ReplaceOptions(){IsUpsert = true}, cancellationToken: stoppingToken);
                        _logger.LogInformation("Inserted ProductScrapingRecord for site {SiteName}", record.SiteName);
                        await _statisticsService.WriteScrapingStateAsync(record.SiteName, ScrapingState.Success, "Loader", DateTime.Now);
                    }
                    else
                    {
                        await _statisticsService.WriteScrapingStateAsync("Unknown", ScrapingState.Failed, "Loader", DateTime.Now);
                        var errorMsg = "Received null or invalid ProductScrapingRecord from Kafka";
                        _logger.LogError(errorMsg);
                        await CreateDeadLetterMsg(stoppingToken, result, errorMsg);
                    }
                    
                }
                catch (Exception ex)
                {
                    await _statisticsService.WriteScrapingStateAsync("Unknown", ScrapingState.Failed, "Loader", DateTime.Now);
                    var errorMsg = $"Error processing message: {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    await CreateDeadLetterMsg(stoppingToken, result, errorMsg);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        finally
        {
            _consumer?.Close();
        }
    }
    private async Task CreateDeadLetterMsg(CancellationToken stoppingToken, ConsumeResult<Null, string> result, string errorMsg)
    {
        var deadLetter = new
        {
            OriginalMessage = result.Message.Value,
            ServiceName = "SiteTransformer",
            Error = errorMsg,
            Timestamp = DateTime.UtcNow
        };
        var deadLetterTopic = _configuration["Kafka:DeadLetterTopic"] ?? "dead-letter-topic";
        await WriteMessageToKafka(stoppingToken, deadLetter, deadLetterTopic);
    }

    private async Task WriteMessageToKafka(CancellationToken stoppingToken, object msg, string topic)
    {
        var dataJson = JsonSerializer.Serialize(msg);
        var message = new Message<Null, string> { Value = dataJson };
        //var messageSize = System.Text.Encoding.UTF8.GetByteCount(scrapingDataJson);
        //_logger.LogWarning("Kafka message size: {Size} bytes", messageSize);
        await _producer.ProduceAsync(topic, message, stoppingToken);
    }


    private void CreateKafkaConsumerAndSubscribe()
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:30092";
        var topic = "product-scraping-data";
        var groupId = _configuration["Kafka:ConsumerGroup"] ?? "data-loader-group";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<Null, string>(config).Build();
        _consumer.Subscribe(topic);

        _logger.LogInformation("DataLoader started, subscribing to topic: {Topic}", topic);
    }

    private IProducer<Null, string> CreateProducer()
    {
        return new ProducerBuilder<Null, string>(CreateKafkaProducerConfig()).Build();
    }

    private ProducerConfig CreateKafkaProducerConfig()
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ??
                               "localhost:30092",
            CompressionType = CompressionType.Gzip,
            MessageMaxBytes = 52428800,
        };
        return producerConfig;
    }
}
