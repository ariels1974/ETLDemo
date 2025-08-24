namespace SiteScraper;

public abstract class ScrapingParameters
{
    public string SiteName { get; set; }
}

public class GraphQLScrapingParameters : ScrapingParameters
{
    public string Query { get; set; } = string.Empty;
    public object? Variables { get; set; }
    public string URL { get; set; }
    public string? OperationName { get; set; }
    public dynamic? DataType { get; set; }
}

public class DynamicHtmlScrapingParameters : ScrapingParameters
{
    public string Url { get; set; } = string.Empty;
    public string? XPathOrSelector { get; set; }
}

public class HeadlessBrowserScrapingParameters :ScrapingParameters
{
    public string SiteAddress { get; set; }
}

public partial class Worker
{
    public record ScrapingData(string Site, string Html, DateTime DateTime);
}