namespace GraphQLClient
{
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class GraphQLClient
    {
        private readonly HttpClient _client;
        private readonly string _endpoint;

        public GraphQLClient(string endpoint)
        {
            _endpoint = endpoint;
            _client = new HttpClient();

            // Copy headers from DevTools
            _client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Accept", "*/*");
            //_client.DefaultRequestHeaders.Add("Content-Type", "application/json");
        }

        public async Task<T> QueryAsync<T>(string query, object variables = null, string operationName = null)
        {
            var request = new
            {
                query = query,
                variables = variables,
                operationName = operationName
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(_endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GraphQLResponse<T>>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result.Errors != null && result.Errors.Any())
            {
                throw new Exception($"GraphQL errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
            }

            return result.Data;
        }

        public async Task<string> QueryAsyncAsString(string query, object variables = null, string operationName = null)
        {
            var request = new
            {
                query = query,
                variables = variables,
                operationName = operationName
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(_endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
           return responseJson;
        }
    }

    public class GraphQLResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("errors")]
        public GraphQLError[] Errors { get; set; }
    }

    public class GraphQLError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("locations")]
        public GraphQLLocation[] Locations { get; set; }

        [JsonPropertyName("path")]
        public string[] Path { get; set; }
    }
    public class GraphQLLocation
    {
        [JsonPropertyName("line")]
        public int Line { get; set; }

        [JsonPropertyName("column")]
        public int Column { get; set; }
    }
}
