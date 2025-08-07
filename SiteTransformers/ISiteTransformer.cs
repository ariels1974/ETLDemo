namespace SiteTransformers;

public record ProductScrapingRecord(
    string Category,
    string Price,
    string SerialNumber,
    string SiteName,
    string Description,
    string SubCategory,
    DateTime DateTime
);

public interface ISiteTransformer
{
    ProductScrapingRecord Transform(string message);
}