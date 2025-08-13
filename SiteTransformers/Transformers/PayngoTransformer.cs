using HtmlAgilityPack;

namespace SiteTransformers.Transformers;

public class PayngoTransformer : ISiteTransformer
{
    public List<ProductScrapingRecord> Transform(string htmlContent, string site)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var products = doc.DocumentNode.SelectNodes(".//li[contains(@class, 'item product product-item')]");

        var productScrapingRecords = new List<ProductScrapingRecord>();
        foreach (var product in products)
        {
            var description = product.SelectSingleNode(".//strong[contains(@class,'product name product-item-name product_name')]");

            var priceHtml = product.SelectSingleNode(".//span[contains(@id, 'product-price')]");
            var price = priceHtml.SelectSingleNode(".//span[contains(@class, 'price')]");
            var priceText = price?.InnerText
                .Replace("&nbsp;", "")        // Remove HTML entity if present
                .Replace("\u00A0", "")        // Remove Unicode non-breaking space
                .Trim();

            var a = price.InnerText;
            productScrapingRecords.Add(new ProductScrapingRecord(
                    Category: "Scooters-Bicycles",
                    Price: priceText,
                    SerialNumber: "B456",
                    SiteName: site,
                    Description: description.InnerText,
                    SubCategory: "Electric-Scooter",
                    DateTime: DateTime.Now
                
            ));
        }

        return productScrapingRecords;
    }
}