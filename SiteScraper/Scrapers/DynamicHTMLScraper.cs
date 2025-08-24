using System.Text.Json;
using HtmlAgilityPack;

namespace SiteScraper.Scrapers;

public class DynamicHTMLScraper : ISiteScraper
{
    private readonly KafkaSenderHelper _kafkaSenderHelper;

    public DynamicHTMLScraper(KafkaSenderHelper kafkaSenderHelper)
    {
        _kafkaSenderHelper = kafkaSenderHelper;
    }
    private readonly HttpClient client = new();
    public async Task ScrapeSite(ScrapingParameters scrapingParameters, CancellationToken cancellationToken)
    {
        try
        {
            if (scrapingParameters is not DynamicHtmlScrapingParameters dynamicHtmlParams) throw new ArgumentException("Invalid scraping parameters for DynamicHTMLScraper");

            var response = await client.GetAsync(dynamicHtmlParams.Url);

            // Ensure the request was successful before proceeding.
            response.EnsureSuccessStatusCode();

            // 2. Read the response content as a string.
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            // Load the content into a new Html Agility Pack document.
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(responseBody);

            // 3. Use Html Agility Pack to scrape the data.
            // **IMPORTANT: Replace this with the XPath or CSS selector for your data.**
            //var targetNode = htmlDoc.DocumentNode.SelectSingleNode("//h1[@class='dynamic-title']");
            var products = htmlDoc.DocumentNode.SelectSingleNode(dynamicHtmlParams.XPathOrSelector);
            if (products == null)
            {
                await _kafkaSenderHelper.CreateDeadLetterMsg(cancellationToken, null, "No data found using the provided XPath or CSS selector.");
                throw new Exception("No data found using the provided XPath or CSS selector.");
            }

            var data = new
            {
                Data = products.InnerHtml,
                SiteName = dynamicHtmlParams.SiteName,
                ScrapeTime = DateTime.UtcNow,
                ScrapingMethod = "DynamicHTML"
            };
            //write the data to Kafka
            await _kafkaSenderHelper.WriteMessageToKafka(cancellationToken, data, "scraping-data");
        }
        catch (Exception e)
        {
            await _kafkaSenderHelper.CreateDeadLetterMsg(cancellationToken, null, e.Message);
            throw;
        }
    }
}