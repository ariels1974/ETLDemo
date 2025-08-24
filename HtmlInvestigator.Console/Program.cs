using HtmlAgilityPack;
using HtmlInvestigator.Console;
using System.Text.Json;

// See https://aka.ms/new-console-template for more information
async Task ExtractFromStaticContent()
{
    Console.WriteLine("Enter path to HTML file (or press Enter for sample.html):");
    var htmlPath =
        @"c:\Users\ariels\source\repos\ETLDemo\SiteScraper\bin\Debug\net9.0\ScrapedHtml\ALM_20250811075649019.html";
    if (string.IsNullOrWhiteSpace(htmlPath))
        htmlPath = "sample.html";

    if (!File.Exists(htmlPath))
    {
        Console.WriteLine($"HTML file not found: {htmlPath}");
        return;
    }

    var html = await File.ReadAllTextAsync(htmlPath);
    var doc = new HtmlDocument();
    doc.LoadHtml(html);

    Console.WriteLine($"Loaded HTML file: {htmlPath}");
    Console.WriteLine($"Root node: {doc.DocumentNode.Name}");
//PrintNodes(doc.DocumentNode, 0);

    var products =
        doc.DocumentNode.SelectNodes("//div[contains(@class, 'item-root-2AI content-start gap-y-xs h-full')]");
    if (products != null)
    {
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
                .Trim(); // Remove leading/trailing whitespace
            Console.WriteLine($"Name = {productName?.InnerText} - Price = {priceText}");
        }
    }
}

async Task GetGraphqlContent()
{
    GraphQLClient.GraphQLClient graphQLClient = new("https://www.alm.co.il/graphql");
//var query = @"query+GetCategories($id:String!$pageSize:Int!$currentPage:Int!$filters:ProductAttributeFilterInput!$sort:ProductAttributeSortInput){categories(filters:{category_uid:{in:[$id]}}){items{uid+...CategoryFragment+__typename}__typename}products(pageSize:$pageSize+currentPage:$currentPage+filter:$filters+sort:$sort){...ProductsFragment+__typename}}fragment+CategoryFragment+on+CategoryTree{uid+meta_title+meta_keywords+meta_description+__typename}fragment+ProductsFragment+on+Products{items{uid+id+...ProductFragment+__typename}page_info{total_pages+current_page+__typename}total_count+__typename}fragment+ProductFragment+on+ProductInterface{name+product_promotions_label+item_category+item_category2+item_category3+item_category4+product_brand{brand_image_url+brand_link+item_brand+__typename}price_range{maximum_price{final_price{currency+value+__typename}regular_price{currency+value+__typename}discount{amount_off+__typename}__typename}minimum_price{final_price{currency+value+__typename}regular_price{currency+value+__typename}discount{amount_off+__typename}__typename}__typename}eilat_price{maximum_price{final_price{currency+value+__typename}regular_price{currency+value+__typename}discount{amount_off+__typename}__typename}minimum_price{final_price{currency+value+__typename}regular_price{currency+value+__typename}discount{amount_off+__typename}__typename}__typename}sku+product_label_amasty{product_page{name+text+image_src+position+priority+is_single+active_from+active_to+alt_tag+image_size+color+__typename}category_page{name+text+image_src+position+priority+is_single+active_from+active_to+alt_tag+image_size+color+__typename}__typename}small_image{url+__typename}short_description{html+__typename}starting_bid_price+is_prestige+stock_status+rating_summary+__typename+url_key}";
//query = query.Replace("+", " ");
//var query = @"query+GetCategories($id:String!$pageSize:Int!$currentPage:Int!$filters:ProductAttributeFilterInput!$sort:ProductAttributeSortInput){categories(filters:{category_uid:{in:[$id]}}){items{uid+...CategoryFragment+__typename}__typename}products(pageSize:$pageSize+currentPage:$currentPage+filter:$filters+sort:$sort){...ProductsFragment+__typename}}fragment+CategoryFragment+on+CategoryTree{uid+meta_title+meta_keywords+meta_description+__typename}fragment+ProductsFragment+on+Products{items{uid+id+...ProductFragment+__typename}page_info{total_pages+current_page+__typename}total_count+__typename}fragment+ProductFragment+on+ProductInterface{name+product_promotions_label+item_category+item_category2+item_category3+item_category4+product_brand{brand_image_url+brand_link+item_brand+__typename}price_range{maximum_price{final_price{currency+value+__typename}regular_price{currency+value+__typename}discount{amount_off+__typename}__typename}minimum_price{final_price{currency+value+__typename}regular_price{currency+value+__typename}discount{amount_off+__typename}__typename}__typename}eilat_price{maximum_price{final_price{currency+value+__typename}regular_price{currency+value+__typename}discount{amount_off+__typename}__typename}minimum_price{final_price{currency+value+__typename}regular_price{currency+value+__typename}discount{amount_off+__typename}__typename}__typename}sku+product_label_amasty{product_page{name+text+image_src+position+priority+is_single+active_from+active_to+alt_tag+image_size+color+__typename}category_page{name+text+image_src+position+priority+is_single+active_from+active_to+alt_tag+image_size+color+__typename}__typename}small_image{url+__typename}short_description{html+__typename}starting_bid_price+is_prestige+stock_status+rating_summary+__typename+url_key}";
    var query = @"
query GetCategories($pageSize:Int! $currentPage:Int! $filters:ProductAttributeFilterInput! $sort:ProductAttributeSortInput) {
        products(pageSize:$pageSize currentPage:$currentPage filter:$filters sort:$sort) {
        ...ProductsFragment
        __typename
    }
}

fragment ProductsFragment on Products {
    items {
        id
        ...ProductFragment
        __typename
    }
    page_info {
        total_pages
        current_page
        __typename
    }
    total_count
    __typename
}

fragment ProductFragment on ProductInterface {
    name
    product_brand {
        brand_image_url
        brand_link
        item_brand
        __typename
    }
    price_range {
        maximum_price {
            final_price {
                currency
                value
                __typename
            }
            regular_price {
                currency
                value
                __typename
            }
            discount {
                amount_off
                __typename
            }
            __typename
        }
        minimum_price {
            final_price {
                currency
                value
                __typename
            }
            regular_price {
                currency
                value
                __typename
            }
            discount {
                amount_off
                __typename
            }
            __typename
        }
        __typename
    }
    sku
    stock_status
    __typename
}";
    var variables = new
    {
        currentPage = 1,
        id = "MzA=",
        filters = new
            { category_uid = new { eq = "MzA=" } },
        pageSize = 12,
        sort = new { relevance = "DESC" }
    };

    // Save the query to a JSON file
    var queryObj = new { query };
    var json = JsonSerializer.Serialize(queryObj, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync("GraphQLQuery.json", json);
    var result = await graphQLClient.QueryAsync<CategoryProductData>(query, variables, "GetCategories");
}

Console.WriteLine("Hello, World!");

await GetGraphqlContent();

//await ExtractFromStaticContent();
var dynamicContentScraper = new DynamicContentScraper();
await dynamicContentScraper.Scrape();

//PrintNodes(div, 0);

void PrintNodes(HtmlNode node, int indent)
{
    var indentStr = new string(' ', indent * 2);
    var attrs = node.HasAttributes ? " " + string.Join(" ", node.Attributes.Select(a => $"{a.Name}=\"{a.Value}\"")) : "";
    Console.WriteLine($"{indentStr}<{node.Name}{attrs}>");
    foreach (var child in node.ChildNodes)
    {
        PrintNodes(child, indent + 1);
    }
}
