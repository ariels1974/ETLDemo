using Confluent.Kafka;
using HtmlAgilityPack;

namespace ETLDemoConsumer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private IConsumer<Null, string>? _consumer;
        private readonly HttpClient _httpClient = new();

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:30092";
            var topic = _configuration["Kafka:Topic"] ?? "user-events";
            var groupId = _configuration["Kafka:ConsumerGroup"] ?? "demo-consumer-group";

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<Null, string>(config).Build();
            _consumer.Subscribe(topic);

            _logger.LogInformation("Kafka consumer started, subscribing to topic: {Topic}", topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = _consumer.Consume(stoppingToken);
                        if (result != null)
                        {
                            var url = result.Message.Value;
                            _logger.LogInformation("Consumed URL: {Url}", url);
                            try
                            {
                                var html = await _httpClient.GetStringAsync(url);
                                var doc = new HtmlDocument();
                                doc.LoadHtml(html);
                                var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
                                _logger.LogInformation("Scraped title: {Title}", title ?? "(no title found)");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to fetch or parse URL: {Url}", url);
                            }
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error");
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
