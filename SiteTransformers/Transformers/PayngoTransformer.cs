using HtmlAgilityPack;

namespace SiteTransformers.Transformers;

public class PayngoTransformer : ISiteTransformer
{
    public ProductScrapingRecord Transform(string htmlContent)
    {
        var doc = new HtmlDocument();

        doc.LoadHtml(htmlContent);
        var productNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'item-root')]");

        // Example: parse message and fill ProductScrapingRecord for SiteB
        return new ProductScrapingRecord(
            Category: "ExampleCategoryB",
            Price: "200",
            SerialNumber: "B456",
            SiteName: "Payngo",
            Description: htmlContent.ToLower(),
            SubCategory: "ExampleSubB",
            DateTime: System.DateTime.UtcNow
        );
    }
}