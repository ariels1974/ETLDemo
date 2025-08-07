namespace SiteScraper;

public partial class Worker
{
    public record ScrapingData(string Site, string Html, DateTime DateTime);
}