using HtmlAgilityPack;

namespace SiteTransformers.Transformers;

public class PayngoTransformer : ISiteTransformer
{
    public List<ProductScrapingRecord> Transform(string method, string data, string site)
    {
        List<ProductScrapingRecord> productScrapingRecords = new List<ProductScrapingRecord>();
        Transform(data, productScrapingRecords);
      
        return productScrapingRecords;
    }
    
    private static void Transform(string data, List<ProductScrapingRecord> scrapingRecords)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(data);

        var products = doc.DocumentNode.SelectNodes(".//li[contains(@class, 'item product product-item')]");

        foreach (var product in products)
        {
            var description =
                product.SelectSingleNode(".//strong[contains(@class,'product name product-item-name product_name')]");

            var priceHtml = product.SelectSingleNode(".//span[contains(@id, 'product-price')]");
            var price = priceHtml.SelectSingleNode(".//span[contains(@class, 'price')]");
            var priceText = price?.InnerText
                .Replace("&nbsp;", "") // Remove HTML entity if present
                .Replace("\u00A0", "") // Remove Unicode non-breaking space
                .Trim();

            var a = price.InnerText;
            scrapingRecords.Add(new ProductScrapingRecord(
                Category: "Scooters-Bicycles",
                Price: priceText,
                SerialNumber: "B456",
                SiteName: "Payngo",
                Description: description.InnerText,
                SubCategory: "Electric-Scooter",
                DateTime: DateTime.Now
            ));
        }
    }
}