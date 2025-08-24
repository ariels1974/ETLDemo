using SiteScraper;
using SiteScraper.Scrapers;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<KafkaSenderHelper>();

//builder.Services.AddTransient<HeadlessBrowser>();
//builder.Services.AddTransient<DynamicHTMLScraper>();
//builder.Services.AddTransient<GraphQLScraper>();

// Dynamically register all ISiteScraper implementations
var scraperInterface = typeof(ISiteScraper);
var scraperTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => scraperInterface.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

foreach (var scraperType in scraperTypes)
{
    builder.Services.AddTransient(scraperType); // Optional: for direct injection by concrete type
}

builder.Services.AddSingleton<ScraperFactory>();

var host = builder.Build();
host.Run();
