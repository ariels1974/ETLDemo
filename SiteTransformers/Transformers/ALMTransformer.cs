namespace SiteTransformers.Transformers;
using HtmlAgilityPack;

public class ALMTransformer : ISiteTransformer
{
    public List<ProductScrapingRecord> Transform(string message, string site)
    {
        var results = new List<ProductScrapingRecord>();
        var doc = new HtmlDocument();
        doc.LoadHtml(message);
        var products = doc.DocumentNode.SelectNodes("//div[contains(@class, 'item-root-2AI content-start gap-y-xs h-full')]");
        foreach (var product in products)
        {
            var productName = product.SelectSingleNode(".//a[contains(@class, 'item-name-1cZ text-sm md_px-10 px-4 md_text-lg text-colorDefault')]");
            var productPrice = product.SelectSingleNode(".//div[contains(@class, 'item-price-1Qq text-colorDefault text-primary text-xl')]");
            var priceText = productPrice?.InnerText
                .Replace("&nbsp;", "")        // Remove HTML entity if present
                .Replace("\u00A0", "")        // Remove Unicode non-breaking space
                .Trim();

            results.Add(
                new ProductScrapingRecord(
                    Category: "Electric-Scooters",
                    Price: priceText!,
                    SerialNumber: "A123",
                    SiteName: site,
                    Description: productName.InnerText,
                    SubCategory: "ExampleSubA",
                    DateTime: System.DateTime.Now));

        }

        return results;
    }
}