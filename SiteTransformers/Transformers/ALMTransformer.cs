namespace SiteTransformers.Transformers;

public class ALMTransformer : ISiteTransformer
{
    public ProductScrapingRecord Transform(string message)
    {
        // Example: parse message and fill ProductScrapingRecord for SiteA
        return new ProductScrapingRecord(
            Category: "ExampleCategoryA",
            Price: "100",
            SerialNumber: "A123",
            SiteName: "SiteA",
            Description: message.ToUpper(),
            SubCategory: "ExampleSubA",
            DateTime: System.DateTime.UtcNow
        );
    }
}