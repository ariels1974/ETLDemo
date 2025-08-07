using System.Text.Json;
using Confluent.Kafka;
using MongoDB.Driver;

namespace DataLoader;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private IConsumer<Null, string>? _consumer;
    private IMongoCollection<ProductScrapingRecord>? _collection;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var mongoConn = _configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
        var mongoDb = _configuration["MongoDB:Database"] ?? "ScrapingDb";
        var mongoCollection = _configuration["MongoDB:Collection"] ?? "ProductScrapingRecords";
        var client = new MongoClient(mongoConn);
        var db = client.GetDatabase(mongoDb);
        _collection = db.GetCollection<ProductScrapingRecord>(mongoCollection);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    if (result != null && _collection != null)
                    {
                        var record = JsonSerializer.Deserialize<ProductScrapingRecord>(result.Message.Value);
                        if (record != null)
                        {
                            await _collection.InsertOneAsync(record, cancellationToken: stoppingToken);
                            _logger.LogInformation("Inserted ProductScrapingRecord for site {SiteName}", record.SiteName);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing or storing message");
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
}
