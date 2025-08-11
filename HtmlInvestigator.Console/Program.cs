using HtmlAgilityPack;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Console.WriteLine("Enter path to HTML file (or press Enter for sample.html):");
var htmlPath = @"c:\Users\ariels\source\repos\ETLDemo\SiteScraper\bin\Debug\net9.0\ScrapedHtml\ALM_20250811075649019.html";
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

var products = doc.DocumentNode.SelectNodes("//div[contains(@class, 'item-root-2AI content-start gap-y-xs h-full')]");
if (products != null)
{
    foreach (var product in products)
    {
        var productName = product.SelectSingleNode(".//a[contains(@class, 'item-name-1cZ text-sm md_px-10 px-4 md_text-lg text-colorDefault')]");
        var productPrice = product.SelectSingleNode(".//div[contains(@class, 'item-price-1Qq text-colorDefault text-primary text-xl')]");
        var priceText = productPrice?.InnerText
            .Replace("&nbsp;", "")        // Remove HTML entity if present
            .Replace("\u00A0", "")        // Remove Unicode non-breaking space
            .Trim();                      // Remove leading/trailing whitespace
        Console.WriteLine($"Name = {productName?.InnerText} - Price = {priceText}");
    }
}

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
