using InfluxDB.Client.Api.Domain;

namespace SiteScraper.Scrapers;

public class GraphQLScraper: ISiteScraper
{
    private readonly KafkaSenderHelper _kafkaSenderHelper;

    public GraphQLScraper(KafkaSenderHelper kafkaSenderHelper)
    {
        _kafkaSenderHelper = kafkaSenderHelper;
    }
    public async Task ScrapeSite(ScrapingParameters scrapingParameters, CancellationToken cancellationToken)
    {
        if(scrapingParameters is not GraphQLScrapingParameters GraphQLParams) throw new ArgumentException("Invalid scraping parameters for GraphQLScraper");

        GraphQLClient.GraphQLClient graphQLClient = new(GraphQLParams.URL);

        var result = await graphQLClient.QueryAsyncAsString(GraphQLParams.Query, GraphQLParams.Variables, GraphQLParams.OperationName);
        if(result == null)
        {
            await _kafkaSenderHelper.CreateDeadLetterMsg(cancellationToken, null, "No data returned from GraphQL query.");
            throw new Exception("No data returned from GraphQL query.");
        }

        var data = new
        {
            Data = result,
            SiteName = GraphQLParams.SiteName,
            ScrapeTime = DateTime.UtcNow,
            ScrapingMethod = "GraphQL"
        };
        //send the data to the next stage of processing (transformer)
        await _kafkaSenderHelper.WriteMessageToKafka(cancellationToken, data, "scraping-data");
    }
}