using Confluent.Kafka;
using System.Text.Json;
using Scraping.Statistics;

namespace SiteTransformers
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private IConsumer<Null, string>? _consumer;
        private readonly SiteTransformerFactory _factory = new();
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
                        var doc = JsonDocument.Parse(result.Message.Value);
                        var site = doc.RootElement.GetProperty("Site").GetString() ?? "";
                        var data = doc.RootElement.GetProperty("Html").GetString() ?? "";
                        // Write state: Started
                        await _statisticsService.WriteScrapingStateAsync(site, ScrapingState.Started, "Transformer", DateTime.Now);

                        var transformer = _factory.GetTransformer(site);
                        var transformed = transformer.Transform(data, site);
                        foreach (var item in transformed)
                        {
                            _logger.LogInformation("Transformed data for site {Site}: {@Transformed}", site, item);

                            await WriteMessageToKafka(stoppingToken, item, "product-scraping-data");
                        }
                        await _statisticsService.WriteScrapingStateAsync(site, ScrapingState.Success, "Transformer", DateTime.Now);
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error processing message for site transformation: {ex.Message}";
                        _logger.LogError(ex, errorMsg);
                        await _statisticsService.WriteScrapingStateAsync(result.Message.Value, ScrapingState.Failed, "Transformer", DateTime.Now);
                        await CreateDeadLetterMsg(stoppingToken, result, errorMsg);
                    }
                }
           }
           catch (OperationCanceledException)
           {
               var errorMsg = "Operation was canceled.";
                _logger.LogWarning(errorMsg);
               // Graceful shutdown
           }
           finally
           {
               _consumer.Close();
           }
        }

        private void CreateKafkaConsumerAndSubscribe()
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:30092";
            var topic = "scraping-data";
            var groupId = _configuration["Kafka:ConsumerGroup"] ?? "scraping-data-consumer-group";

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<Null, string>(config).Build();
            _consumer.Subscribe(topic);

            _logger.LogInformation("ScrapingDataConsumer started, subscribing to topic: {Topic}", topic);
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
}
