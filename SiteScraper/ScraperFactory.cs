using SiteScraper.Scrapers;

namespace SiteScraper;

public class ScraperFactory
{
    private readonly IServiceProvider _serviceProvider;
    public ScraperFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public ISiteScraper GetScraper(string scrapingMethod)
    {
        return scrapingMethod switch
        {
            "DynamicHTML" => _serviceProvider.GetRequiredService<DynamicHTMLScraper>(),
            "GraphQL" => _serviceProvider.GetRequiredService<GraphQLScraper>(),
            "HeadLessBrowser" => _serviceProvider.GetRequiredService<HeadlessBrowser>(),
            _ => _serviceProvider.GetRequiredService<HeadlessBrowser>(),
        };
    }
}

public class ScrapingMappingEntry
{
    public string SiteName { get; set; }
    public List<ScrappingMethodEntry> ScrappingMethod { get; set; }
    public string PageUrl { get; set; }
}

public class ScrappingMethodEntry
{
    public string MethodType { get; set; }
    public GraphQLMethod? GraphQL { get; set; }
    public DynamicHTMLMethod? DynamicHTML { get; set; }

}

public class GraphQLMethod
{
    public string? Query { get; set; }
    public object? Variables { get; set; }
    public string? URL { get; set; }
    public string? operationName { get; set; }
    public dynamic? DataType { get; set; }
}

public class DynamicHTMLMethod
{
    public string? ProductNodeQuery { get; set; }
    public string? URL { get; set; }
}