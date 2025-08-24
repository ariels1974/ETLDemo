using System.Text.Json;
using Newtonsoft.Json;

namespace SiteTransformers.Transformers;
using HtmlAgilityPack;

public partial class ALMTransformer : ISiteTransformer
{
    public List<ProductScrapingRecord> Transform(string method, string data, string site)
    {
        var results = new List<ProductScrapingRecord>();

        switch (method)
        {
            case "GraphQL":
               TransformGraphQL(data, results);
                break;
            default:
                TransformHeadlessBrowser(data, site, results);
                break;
        }

        return results;
    }

    private static void TransformGraphQL(string data, List<ProductScrapingRecord> productScrapingRecords)
    {
        var products = System.Text.Json.JsonSerializer.Deserialize<GraphQLResponse<CategoryProductData>>(data, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true

        }) ?? new GraphQLResponse<CategoryProductData>();

        foreach (var categoryProductData in products.Data.Products.Items)
        {
            productScrapingRecords.Add(new ProductScrapingRecord(
                Category: "Scooters-Bicycles",
                Price: categoryProductData.PriceRange.MaximumPrice.FinalPrice.Value.ToString("C2"),
                SerialNumber: string.Empty,
                SiteName: "ALM",
                Description: categoryProductData.Name,
                SubCategory: "Electric-Scooter",
                DateTime: System.DateTime.UtcNow
            ));
        }
    }

    private static void TransformHeadlessBrowser(string data, string site, List<ProductScrapingRecord> results)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(data);
        var products =
            doc.DocumentNode.SelectNodes("//div[contains(@class, 'item-root-2AI content-start gap-y-xs h-full')]");
        foreach (var product in products)
        {
            var productName =
                product.SelectSingleNode(
                    ".//a[contains(@class, 'item-name-1cZ text-sm md_px-10 px-4 md_text-lg text-colorDefault')]");
            var productPrice =
                product.SelectSingleNode(
                    ".//div[contains(@class, 'item-price-1Qq text-colorDefault text-primary text-xl')]");
            var priceText = productPrice?.InnerText
                .Replace("&nbsp;", "") // Remove HTML entity if present
                .Replace("\u00A0", "") // Remove Unicode non-breaking space
                .Trim();

            results.Add(new ProductScrapingRecord(
                    Category: "Electric-Scooters",
                    Price: priceText!,
                    SerialNumber: "A123",
                    SiteName: site,
                    Description: productName.InnerText,
                    SubCategory: "ExampleSubA",
                    DateTime: System.DateTime.Now));
        }
    }
}