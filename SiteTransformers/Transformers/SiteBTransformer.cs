namespace SiteTransformers.Transformers;

public class SiteBTransformer : ISiteTransformer
{
    public ProductScrapingRecord Transform(string message)
    {
        // Example: parse message and fill ProductScrapingRecord for SiteB
        return new ProductScrapingRecord(
            Category: "ExampleCategoryB",
            Price: "200",
            SerialNumber: "B456",
            SiteName: "SiteB",
            Description: message.ToLower(),
            SubCategory: "ExampleSubB",
            DateTime: System.DateTime.UtcNow
        );
    }
}