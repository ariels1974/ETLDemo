using Confluent.Kafka;
using StealthWebScraper;
using System.Text.Json;

namespace SiteScraper;
public partial class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private IConsumer<Null, string>? _consumer;
    private readonly IProducer<Null, string>? _producer;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _producer = CreateProducer();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer = CreateKafkaConsumer();
        var scrapingRequestsTopic = _configuration["Kafka:ConsumerTopic"] ?? "scraping-requests";
        var scrapingDataTopic = _configuration["Kafka:ProducerTopic"] ?? "scraping-data";
        _consumer.Subscribe(scrapingRequestsTopic);

        _logger.LogInformation("SiteScraper started, subscribing to topic: {Topic}", scrapingRequestsTopic);
        try
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                var result = _consumer.Consume(stoppingToken);

                try
                {
                    var scheduleConfig = JsonSerializer.Deserialize<SiteScheduleConfig>(result.Message.Value);
                    if (scheduleConfig != null)
                    {
                        _logger.LogInformation(
                            "Consumed SiteScheduleConfig: SiteName={SiteName}, SiteAddress={SiteAddress}",
                            scheduleConfig.SiteName, scheduleConfig.SiteAddress);

                        var pageContent = await GetSiteData(scheduleConfig);

                        if (pageContent != null)
                        {
                            var scrapingData = new ScrapingData(scheduleConfig.SiteName, pageContent.Html,
                                DateTime.Now);

                            await WriteMessageToKafka(stoppingToken, scrapingData, scrapingDataTopic);
                            _logger.LogInformation("Sent scraping-data for {Site} to Kafka", scrapingData.Site);
                        }
                        else
                        {
                            var errorMsg = $"Failed to scrape data for site: {scheduleConfig.SiteName}";
                            _logger.LogError(errorMsg);
                            await CreateDeadLetterMsg(stoppingToken, result, errorMsg);
                        }
                    }
                    else
                    {
                        var errorMsg = "Received null or invalid SiteScheduleConfig from Kafka";
                        _logger.LogWarning(errorMsg);
                        await CreateDeadLetterMsg(stoppingToken, result, errorMsg);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message, sending to dead letter topic");
                    await CreateDeadLetterMsg(stoppingToken, result, ex.ToString());
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SiteScraper worker stopped by cancellation request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in SiteScraper worker");
        }
        finally
        {
            DisposeKafkaResources();
            _logger.LogInformation("SiteScraper worker stopped.");
        }
    }

    private void DisposeKafkaResources()
    {
        _consumer?.Close();
        _producer?.Dispose();
        _consumer?.Dispose();
    }

    public override void Dispose()
    {
        DisposeKafkaResources();
        base.Dispose();
    }
    private async Task CreateDeadLetterMsg(CancellationToken stoppingToken, ConsumeResult<Null, string> result, string errorMsg)
    {
        var deadLetter = new
        {
            OriginalMessage = result.Message.Value,
            ServiceName = "SiteScraper",
            Error = errorMsg,
            Timestamp = DateTime.UtcNow
        };
        var deadLetterTopic = _configuration["Kafka:DeadLetterTopic"] ?? "dead-letter-topic";
        await WriteMessageToKafka(stoppingToken, deadLetter, deadLetterTopic);
    }

    private async Task WriteMessageToKafka(CancellationToken stoppingToken, object msg,string topic)
    {
        var dataJson = JsonSerializer.Serialize(msg);
        var message = new Message<Null, string> { Value = dataJson };
        //var messageSize = System.Text.Encoding.UTF8.GetByteCount(scrapingDataJson);
        //_logger.LogWarning("Kafka message size: {Size} bytes", messageSize);
        await _producer.ProduceAsync(topic, message, stoppingToken);
    }

    private static async Task<PageContent?> GetSiteData(SiteScheduleConfig scheduleConfig)
    {
        using var scraper = new StealthWebScraper.StealthWebScraper();
        // Create stealth driver
        await scraper.CreateStealthDriver();
        var res = await scraper.NavigateWithRetry(scheduleConfig.SiteAddress);
        if (res)
        {
            return await scraper.ExtractAllContent();
        }

        return null;
    }

    private IConsumer<Null,string> CreateKafkaConsumer()
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:30092";
        var groupId = _configuration["Kafka:ConsumerGroup"] ?? "site-scraper-group";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Latest
        };

        return new ConsumerBuilder<Null, string>(config).Build();
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


    private static async Task ExportToFile(CancellationToken stoppingToken, string productName, string content)
    {
        var outputDir = Path.Combine(AppContext.BaseDirectory, "ScrapedHtml");
        Directory.CreateDirectory(outputDir);
        var safeFileName = string.Join("_", productName.Split(Path.GetInvalidFileNameChars()));
        var outputPath = Path.Combine(outputDir, $"{safeFileName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.html");
        await File.WriteAllTextAsync(outputPath, content, stoppingToken);
    }
}