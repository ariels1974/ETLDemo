using InfluxDB.Client.Api.Domain;

namespace SiteScraper.Scrapers;

public class GraphQLScraper: ISiteScraper
{
    private readonly KafkaSenderHelper _kafkaSenderHelper;

    GraphQLScraper(KafkaSenderHelper kafkaSenderHelper)
    {
        _kafkaSenderHelper = kafkaSenderHelper;
    }
    public async Task ScrapeSite(ScrapingParameters scrapingParameters, CancellationToken cancellationToken)
    {
        if(scrapingParameters is not GraphQLScrapingParameters GraphQLParams) throw new ArgumentException("Invalid scraping parameters for GraphQLScraper");

        GraphQLClient.GraphQLClient graphQLClient = new(GraphQLParams.URL);

        var method = typeof(GraphQLClient.GraphQLClient)
            .GetMethod("QueryAsync")
            .MakeGenericMethod(GraphQLParams.DataType);

        var task = (Task)method.Invoke(graphQLClient, new object[] { GraphQLParams.Query, GraphQLParams.Variables, GraphQLParams.OperationName });
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty.GetValue(task); 
        if(result == null)
        {
            await _kafkaSenderHelper.CreateDeadLetterMsg(cancellationToken, null, "No data returned from GraphQL query.");
            throw new Exception("No data returned from GraphQL query.");
        }

        var data = new
        {
            GraphQLData = result,
            SiteName = GraphQLParams.SiteName,
            ScrapeTime = DateTime.UtcNow,
            ScrapingMethod = "GraphQL"
        };
        //send the data to the next stage of processing (transformer)
        await _kafkaSenderHelper.WriteMessageToKafka(cancellationToken, data, "scraping-data");
    }
}