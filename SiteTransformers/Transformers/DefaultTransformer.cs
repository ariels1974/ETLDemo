namespace SiteTransformers.Transformers;

public class DefaultTransformer : ISiteTransformer
{
    public List<ProductScrapingRecord> Transform(string message, string site)
    {
        // Default mapping
        return new List<ProductScrapingRecord>
            {
                new(
                    Category: "Unknown",
                    Price: string.Empty,
                    SerialNumber: string.Empty,
                    SiteName: "Unknown",
                    Description: message,
                    SubCategory: string.Empty,
                    DateTime: System.DateTime.UtcNow
                )
            };
    }
}