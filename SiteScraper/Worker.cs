using Confluent.Kafka;


namespace SiteScraper;
public partial class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private IConsumer<Null, string>? _consumer;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:30092";
        var topic = _configuration["Kafka:Topic"] ?? "scraping-requests";
        var groupId = _configuration["Kafka:ConsumerGroup"] ?? "site-scraper-group";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<Null, string>(config).Build();
        _consumer.Subscribe(topic);

        _logger.LogInformation("SiteScraper started, subscribing to topic: {Topic}", topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    if (result != null)
                    {
                        var productName = result.Message.Value;
                        _logger.LogInformation("Consumed product name: {ProductName}", productName);
                        
                            var searchUrl = $"https://www.alm.co.il/search.html?query={Uri.EscapeDataString(productName)}";
                        try
                        {
                            using var scraper = new StealthWebScraper.StealthWebScraper();
                            // Create stealth driver
                            await scraper.CreateStealthDriver();
                            var success = await scraper.NavigateWithRetry(searchUrl);

                            if (success)
                            {
                                // Wait for your element (fixed selector)
                                var element = await scraper.WaitForElement("#root"); // or ".root" for class
                                var content = await scraper.ExtractAllContent();
                                await ExportToFile(stoppingToken, productName, content.Html);

                                var scrapingData = new ScrapingData("alm.co.il", content.Html, DateTime.Now);

                                // Send to Kafka on scraping-data topic
                                var producerConfig = new ProducerConfig
                                {
                                    BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:30092"
                                };

                                using var producer = new ProducerBuilder<Null, string>(producerConfig).Build();
                                var scrapingDataJson = System.Text.Json.JsonSerializer.Serialize(scrapingData);
                                var message = new Message<Null, string> { Value = scrapingDataJson };
                                await producer.ProduceAsync("scraping-data", message, stoppingToken);
                                _logger.LogInformation("Sent scraping-data for {Site} to Kafka", scrapingData.Site);
                            }

                            // Do your scraping...
                            //Console.WriteLine("Element found! Content: " + element.Text);
                            //var doc = new HtmlDocument();

                            //doc.LoadHtml(content.Html);
                            //var productNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'item-root')]");
                            //var name = productNode.SelectSingleNode(".//a[contains(@class, 'item-name')]")?.InnerText?.Trim();

                            else
                            {
                                //Console.WriteLine("Failed to navigate after all retries");
                            }
                            // Use explicit wait for the product container to appear
                            //var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                            //wait.Until(ExpectedConditions.ElementExists(By.CssSelector("#root")));

                            //var content = driver.PageSource;

                            // Save HTML as .html file

                            //if (productNode != null)
                            //{

                            //    var price = productNode.SelectSingleNode(".//div[contains(@class, 'item-priceWrapper')]")?.InnerText?.Trim();
                            //    var link = productNode.SelectSingleNode(".//a[@href]")?.GetAttributeValue("href", null);
                            //    if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                            //        link = "https://www.payngo.co.il" + link;
                            //    _logger.LogInformation("Scraped: Name={Name}, Price={Price}, Link={Link}", name, price, link);
                            //}
                            //else
                            //{
                            //    _logger.LogWarning("No product found for: {ProductName}", productName);
                            //}
                        }
                        catch (Exception ex)
                        {
                            //await ExportToFile(stoppingToken, productName, driver.PageSource);
                            _logger.LogError(ex, "Failed to search or scrape site for: {ProductName}", productName);
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

    private static async Task ExportToFile(CancellationToken stoppingToken, string productName, string content)
    {
        var outputDir = Path.Combine(AppContext.BaseDirectory, "ScrapedHtml");
        Directory.CreateDirectory(outputDir);
        var safeFileName = string.Join("_", productName.Split(Path.GetInvalidFileNameChars()));
        var outputPath = Path.Combine(outputDir, $"{safeFileName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.html");
        await File.WriteAllTextAsync(outputPath, content, stoppingToken);
    }
}
