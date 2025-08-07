using System.Text.Json;
using Confluent.Kafka;
using SiteTransformers.Transformers;

namespace SiteTransformers
{
    public class SiteTransformerFactory
    {
        public ISiteTransformer GetTransformer(string site)
        {
            return site switch
            {
                "alm.co.il" => new ALMTransformer(),
                "SiteB" => new SiteBTransformer(),
                _ => new DefaultTransformer(),
            };
        }
    }

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private IConsumer<Null, string>? _consumer;
        private readonly SiteTransformerFactory _factory = new();

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = _consumer.Consume(stoppingToken);
                        if (result != null)
                        {
                            var doc = JsonDocument.Parse(result.Message.Value);
                            var site = doc.RootElement.GetProperty("Site").GetString() ?? "";
                            var data = doc.RootElement.GetProperty("Html").GetString() ?? "";
                            var transformer = _factory.GetTransformer(site);
                            var transformed = transformer.Transform(data);
                            _logger.LogInformation("Transformed data for site {Site}: {@Transformed}", site, transformed);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            finally
            {
                _consumer.Close();
            }
        }
    }
}
