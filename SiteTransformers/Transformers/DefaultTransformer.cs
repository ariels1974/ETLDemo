namespace SiteTransformers.Transformers;

public class DefaultTransformer : ISiteTransformer
{
    public ProductScrapingRecord Transform(string message)
    {
        // Default mapping
        return new ProductScrapingRecord(
            Category: "Unknown",
            Price: string.Empty,
            SerialNumber: string.Empty,
            SiteName: "Unknown",
            Description: message,
            SubCategory: string.Empty,
            DateTime: System.DateTime.UtcNow
        );
    }
}