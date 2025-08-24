using Confluent.Kafka;
using System.Text.Json;
using File = System.IO.File;
using Scraping.Statistics;

namespace SiteScraper;
public partial class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private IConsumer<Null, string>? _consumer;

    private readonly ScrapingStatisticsService _statisticsService;
    private readonly KafkaSenderHelper _kafkaSenderHelper;
    private readonly List<ScrapingMappingEntry> _scrapingMappings;
    private readonly ScraperFactory _scraperFactory;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, ScraperFactory scraperFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _kafkaSenderHelper = new KafkaSenderHelper(configuration);
        _statisticsService = new ScrapingStatisticsService(
            configuration["InfluxDB:Url"],
            configuration["InfluxDB:Token"],
            configuration["InfluxDB:Org"],
            configuration["InfluxDB:Bucket"]
        );
        var mappingJson = File.ReadAllText("Resources/ScrapingMapping.json");
        _scrapingMappings = JsonSerializer.Deserialize<List<ScrapingMappingEntry>>(mappingJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        _scraperFactory = scraperFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer = CreateKafkaConsumer();
        var scrapingRequestsTopic = _configuration["Kafka:ConsumerTopic"] ?? "scraping-requests";
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

                        // Write state: Started
                        await _statisticsService.WriteScrapingStateAsync(scheduleConfig.SiteName, ScrapingState.Started, "Extractor", DateTime.Now);

                        // Try scraping methods in order
                        var mapping = _scrapingMappings.FirstOrDefault(m => m.SiteName == scheduleConfig.SiteName);
                        if (mapping != null)
                        {
                            bool success = false;
                            foreach (var method in mapping.ScrappingMethod)
                            {
                                try
                                {
                                    var scraper = _scraperFactory.GetScraper(method.MethodType);
                                    var parameters = GetScrapingParameters(method, scheduleConfig);
                                    await scraper.ScrapeSite(parameters,stoppingToken);
                                    success = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, $"Scraping method {method.MethodType} failed, trying next if available.");
                                }
                            }
                            if (!success)
                            {
                                var errorMsg = $"All scraping methods failed for site {scheduleConfig.SiteName}";
                                _logger.LogError(errorMsg);
                                await _kafkaSenderHelper.CreateDeadLetterMsg(stoppingToken, result, errorMsg);
                                await _statisticsService.WriteScrapingStateAsync(scheduleConfig.SiteName, ScrapingState.Failed, "Extractor", DateTime.Now);
                            }
                        }
                        else
                        {
                            var errorMsg = $"No scraping mapping found for site {scheduleConfig.SiteName}";
                            _logger.LogWarning(errorMsg);
                            await _kafkaSenderHelper.CreateDeadLetterMsg(stoppingToken, result, errorMsg);
                            await _statisticsService.WriteScrapingStateAsync(scheduleConfig.SiteName, ScrapingState.Failed, "Extractor", DateTime.Now);
                        }
                    }
                    else
                    {
                        var errorMsg = "Received null or invalid SiteScheduleConfig from Kafka";
                        _logger.LogWarning(errorMsg);
                        await _kafkaSenderHelper.CreateDeadLetterMsg(stoppingToken, result, errorMsg);
                        await _statisticsService.WriteScrapingStateAsync("Unknown", ScrapingState.Failed, "Extractor", DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message, sending to dead letter topic");
                    await _kafkaSenderHelper.CreateDeadLetterMsg(stoppingToken, result, ex.ToString());
                    await _statisticsService.WriteScrapingStateAsync("Unknown", ScrapingState.Failed, "Extractor", DateTime.Now);
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

    private static ScrapingParameters GetScrapingParameters(ScrappingMethodEntry method, SiteScheduleConfig scheduleConfig)
    {
        ScrapingParameters parameters = method.MethodType switch
        {
            "DynamicHTML" => new DynamicHtmlScrapingParameters
            {
                Url = method.DynamicHTML?.URL ?? string.Empty,
                XPathOrSelector = method.DynamicHTML?.ProductNodeQuery,
                SiteName = scheduleConfig.SiteName,
            },
            "GraphQL" => new GraphQLScrapingParameters
            {
                Query = method.GraphQL?.Query ?? string.Empty,
                Variables = method.GraphQL?.Variables,
                URL = method.GraphQL?.URL ?? string.Empty,
                OperationName = method.GraphQL?.operationName,
                DataType = method.GraphQL?.DataType,
                SiteName = scheduleConfig.SiteName,
            },
            "HeadlessBrowser" => new HeadlessBrowserScrapingParameters
            {
                SiteAddress = scheduleConfig.SiteAddress,
                SiteName = scheduleConfig.SiteName,
            },
            _ => throw new NotImplementedException()
        };
        return parameters;
    }

    private void DisposeKafkaResources()
    {
        _consumer?.Close();
        _consumer?.Dispose();
    }

    public override void Dispose()
    {
        DisposeKafkaResources();
        base.Dispose();
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

    private static async Task ExportToFile(CancellationToken stoppingToken, string productName, string content)
    {
        var outputDir = Path.Combine(AppContext.BaseDirectory, "ScrapedHtml");
        Directory.CreateDirectory(outputDir);
        var safeFileName = string.Join("_", productName.Split(Path.GetInvalidFileNameChars()));
        var outputPath = Path.Combine(outputDir, $"{safeFileName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.html");
        await File.WriteAllTextAsync(outputPath, content, stoppingToken);
    }
}