using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace Scraping.Statistics;

public enum ScrapingState
{
    Started,
    Success,
    Failed
}

public class ScrapingStatisticsService
{
    private readonly string _url;
    private readonly string _token;
    private readonly string _org;
    private readonly string _bucket;

    public ScrapingStatisticsService(string url, string token, string org, string bucket)
    {
        _url = url;
        _token = token;
        _org = org;
        _bucket = bucket;
    }

    public async Task WriteScrapingStateAsync(string site, ScrapingState state, string component, DateTime timestamp)
    {
        using var influxDBClient = new InfluxDBClient(_url, _token);
        var point = PointData
            .Measurement("scraping_state")
            .Tag("site", site)
            .Tag("component", component)
            .Field("state", state.ToString())
            .Timestamp(timestamp, WritePrecision.Ms);

        var writeApi = influxDBClient.GetWriteApiAsync();
        await writeApi.WritePointAsync(point, _bucket, _org);
    }
}
