using Scraping.Statistics;
using StealthWebScraper;

namespace SiteScraper.Scrapers;

public class HeadlessBrowser : ISiteScraper
{
    private readonly ILogger<HeadlessBrowser> _logger;
    private readonly KafkaSenderHelper _kafkaSenderHelper;
    private readonly ScrapingStatisticsService _statisticsService;
    private readonly string _scrapingDataTopic;

    public HeadlessBrowser(
        ILogger<HeadlessBrowser> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _scrapingDataTopic = configuration["Kafka:ProducerTopic"] ?? "scraping-data";
        _kafkaSenderHelper = new KafkaSenderHelper(configuration);
        _statisticsService = new ScrapingStatisticsService(
            configuration["InfluxDB:Url"],
            configuration["InfluxDB:Token"],
            configuration["InfluxDB:Org"],
            configuration["InfluxDB:Bucket"]
        );
    }
    public async Task ScrapeSite(ScrapingParameters scrapingParameters, CancellationToken cancellationToken)
    {
        try
        {
            if (scrapingParameters is not HeadlessBrowserScrapingParameters headlessBrowserScrapingParameters)
                throw new ArgumentException("Invalid scraping parameters for HeadlessBrowser");
            var pageContent = await GetSiteData(headlessBrowserScrapingParameters.SiteAddress);

            if (pageContent != null)
            {
                var scrapingData = new Worker.ScrapingData(headlessBrowserScrapingParameters.SiteName, pageContent.Html,
                    DateTime.Now);

                var data = new
                {
                    Data = pageContent.Html,
                    SiteName = headlessBrowserScrapingParameters.SiteName,
                    ScrapeTime = DateTime.UtcNow,
                    ScrapingMethod = "HeadlessBrowser"
                };

                await _kafkaSenderHelper.WriteMessageToKafka(cancellationToken, data, _scrapingDataTopic);
                _logger.LogInformation("Sent scraping-data for {Site} to Kafka", scrapingData.Site);
                // Write state: Success
                await _statisticsService.WriteScrapingStateAsync(headlessBrowserScrapingParameters.SiteName,
                    ScrapingState.Success,
                    "Extractor",
                    DateTime.Now);

                //write kafka message
                await _kafkaSenderHelper.WriteMessageToKafka(cancellationToken, pageContent.Html, _scrapingDataTopic);
            }
            else
            {
                var errorMsg = $"Failed to scrape data for site: {headlessBrowserScrapingParameters.SiteName}";
                _logger.LogError(errorMsg);
                await _kafkaSenderHelper.CreateDeadLetterMsg(cancellationToken, null, errorMsg);
                // Write state: Error
                await _statisticsService.WriteScrapingStateAsync(headlessBrowserScrapingParameters.SiteName,
                    ScrapingState.Failed, "Extractor",
                    DateTime.Now);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during scraping");
            await _kafkaSenderHelper.CreateDeadLetterMsg(cancellationToken, null, ex.Message);
            // Write state: Error
            if (scrapingParameters is HeadlessBrowserScrapingParameters headlessParams)
            {
                await _statisticsService.WriteScrapingStateAsync(headlessParams.SiteName,
                    ScrapingState.Failed, "Extractor",
                    DateTime.Now);
            }
        }
       
    }

    private static async Task<PageContent?> GetSiteData(string siteAddress)
    {
        using var scraper = new StealthWebScraper.StealthWebScraper();
        // Create stealth driver
        await scraper.CreateStealthDriver();
        var res = await scraper.NavigateWithRetry(siteAddress);
        if (res)
        {
            return await scraper.ExtractAllContent();
        }

        return null;
    }

}