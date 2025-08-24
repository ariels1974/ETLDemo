using System.Text.Json;
using HtmlAgilityPack;

public class DynamicContentScraper
{
    // Make sure your class or Program.cs has a using System.Net.Http; statement.
        // The HttpClient should be a singleton or reused instance for performance.
    private readonly HttpClient client = new HttpClient();

    public async Task Scrape()
    {
        // The URL of the specific HTML page you found in the network traffic.
        // **IMPORTANT: Replace this with the actual URL you found.**
        string dynamicHtmlUrl = "https://www.payngo.co.il/scooters-bicycles/scooters/electric-scooter.html";

        try
        {
            Console.WriteLine($"Requesting content from {dynamicHtmlUrl}...");

            // 1. Send a GET request to the specific URL.
            // The await keyword means the program will pause here until the request is complete.
            HttpResponseMessage response = await client.GetAsync(dynamicHtmlUrl);

            // Ensure the request was successful before proceeding.
            response.EnsureSuccessStatusCode();

            // 2. Read the response content as a string.
            string responseBody = await response.Content.ReadAsStringAsync();

            // Load the content into a new Html Agility Pack document.
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(responseBody);

            // 3. Use Html Agility Pack to scrape the data.
            // **IMPORTANT: Replace this with the XPath or CSS selector for your data.**
            //var targetNode = htmlDoc.DocumentNode.SelectSingleNode("//h1[@class='dynamic-title']");
            var products = htmlDoc.DocumentNode.SelectSingleNode(".//ol[contains(@class, 'products list items')]");
              
            var a= JsonSerializer.Serialize(products.InnerHtml);


            //htmlDoc.LoadHtml(products.InnerHtml);
            //var items = htmlDoc.DocumentNode.SelectNodes(".//li[contains(@class, 'item product product-item')]");

            var messageSize = System.Text.Encoding.UTF8.GetByteCount(a);
            //if (targetNode != null)
            //{
            //    // Get the inner text and print it to the console.
            //    string scrapedData = targetNode.InnerText;
            //    Console.WriteLine("--- Successfully Scraped Dynamic Data ---");
            //    Console.WriteLine(scrapedData);
            //    Console.WriteLine("---------------------------------------");
            //}
            //else
            //{
            //    Console.WriteLine("Could not find the target element in the HTML response.");
            //}
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error requesting URL: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }
}
