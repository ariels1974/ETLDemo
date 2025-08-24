namespace SiteScraper;

public interface ISiteScraper
{
    Task ScrapeSite(ScrapingParameters scrapingParameters, CancellationToken cancellationToken);
}