using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace StealthWebScraper;

public class StealthWebScraper
{
    private ChromeDriver driver;
    private WebDriverWait wait;
    private Random random = new();

    public async Task<ChromeDriver> CreateStealthDriver()
    {
        var options = new ChromeOptions();

        // Remove automation indicators
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);
        options.AddArgument("--disable-blink-features=AutomationControlled");

        // Stealth arguments
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-plugins");
        options.AddArgument("--disable-images"); // Optional: faster loading

        // Realistic user agent
        options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        options.AddArgument("--accept-language=en-US,en;q=0.9");

        // Create driver
        driver = new ChromeDriver(options);
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

        // Remove automation traces
        await RemoveAutomationTraces();

        return driver;
    }

    private async Task RemoveAutomationTraces()
    {
        // Execute JavaScript to hide automation
        var scripts = new List<string>
        {
            "Object.defineProperty(navigator, 'webdriver', {get: () => undefined})",
            "Object.defineProperty(navigator, 'plugins', {get: () => [1, 2, 3, 4, 5]})",
            "Object.defineProperty(navigator, 'languages', {get: () => ['en-US', 'en']})",
            "Object.defineProperty(navigator, 'platform', {get: () => 'Win32'})",
            "window.chrome = { runtime: {} };"
        };

        foreach (var script in scripts)
        {
            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript(script);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Script execution failed: {ex.Message}");
            }
        }
    }

    public async Task<bool> NavigateWithRetry(string url, int maxRetries = 3)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                Console.WriteLine($"Attempt {i + 1} to navigate to: {url}");

                // Human-like delay before navigation
                await HumanDelay(2000, 5000);

                driver.Navigate().GoToUrl(url);

                // Wait for page to load
                await WaitForPageLoad();

                // Check for Incapsula challenge
                if (await HandleIncapsulaChallenge())
                {
                    Console.WriteLine("Incapsula challenge handled, retrying...");
                    continue;
                }

                Console.WriteLine("Navigation successful!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Navigation attempt {i + 1} failed: {ex.Message}");

                if (i < maxRetries - 1)
                {
                    // Wait longer between retries
                    await HumanDelay(30000, 120000); // 30s-2min

                    // Optionally restart driver
                    if (i > 0)
                    {
                        await RestartDriver();
                    }
                }
            }
        }

        return false;
    }

    private async Task<bool> HandleIncapsulaChallenge()
    {
        try
        {
            var pageSource = driver.PageSource.ToLower();

            if (pageSource.Contains("incapsula") ||
                pageSource.Contains("incident id") ||
                pageSource.Contains("request unsuccessful"))
            {
                Console.WriteLine("Incapsula challenge detected, waiting...");

                // Wait for challenge to potentially resolve
                await HumanDelay(10000, 20000); // 10-20 seconds

                // Try refreshing
                driver.Navigate().Refresh();
                await HumanDelay(5000, 10000); // 5-10 seconds

                // Check again
                pageSource = driver.PageSource.ToLower();
                if (pageSource.Contains("incapsula") || pageSource.Contains("incident id"))
                {
                    return true; // Still blocked, need retry
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling Incapsula challenge: {ex.Message}");
        }

        return false;
    }

    private async Task WaitForPageLoad()
    {
        try
        {
            // Wait for document ready state
            wait.Until(driver =>
                ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));

            // Additional wait for dynamic content
            await HumanDelay(2000, 4000);
        }
        catch (WebDriverTimeoutException)
        {
            Console.WriteLine("Page load timeout, continuing anyway...");
        }
    }

    public async Task<IWebElement> WaitForElement(string cssSelector, int timeoutSeconds = 30)
    {
        try
        {
            // Fix common selector issues
            var fixedSelector = FixCssSelector(cssSelector);
            Console.WriteLine($"Waiting for element: {fixedSelector}");

            var elementWait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            var element = elementWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions
                .ElementIsVisible(By.CssSelector(fixedSelector)));

            // Human-like delay before interacting
            await HumanDelay(500, 1500);

            return element;
        }
        catch (WebDriverTimeoutException)
        {
            Console.WriteLine($"Element not found: {cssSelector}");
            await DebugAvailableElements(cssSelector);
            throw;
        }
    }

    private string FixCssSelector(string selector)
    {
        // Fix common selector issues
        if (selector == "root")
        {
            return "#root"; // Assume they meant ID
        }

        return selector;
    }

    private async Task DebugAvailableElements(string originalSelector)
    {
        try
        {
            Console.WriteLine("Debugging available elements...");
            Console.WriteLine($"Page title: {driver.Title}");
            Console.WriteLine($"Current URL: {driver.Url}");

            // Look for elements with 'root' in id or class
            var rootElements = driver.FindElements(By.XPath("//*[contains(@id,'root') or contains(@class,'root')]"));

            Console.WriteLine($"Found {rootElements.Count} elements with 'root' in id or class:");
            foreach (var elem in rootElements)
            {
                var id = elem.GetAttribute("id");
                var className = elem.GetAttribute("class");
                Console.WriteLine($"  Tag: {elem.TagName}, ID: '{id}', Class: '{className}'");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Debug failed: {ex.Message}");
        }
    }

    private async Task HumanDelay(int minMs = 1000, int maxMs = 3000)
    {
        var delay = random.Next(minMs, maxMs);
        await Task.Delay(delay);
    }

    private async Task RestartDriver()
    {
        try
        {
            Console.WriteLine("Restarting driver...");
            driver?.Quit();
            await Task.Delay(random.Next(5000, 10000)); // Wait before restart
            await CreateStealthDriver();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Driver restart failed: {ex.Message}");
        }
    }

    public async Task<PageContent> ExtractAllContent()
    {
        try
        {
            Console.WriteLine("Extracting page content...");
            await HumanDelay(1000, 2000); // Human-like delay

            var content = new PageContent
            {
                Title = driver.Title,
                Url = driver.Url,
                Html = driver.PageSource,
                Text = ExtractTextContent(),
                Links = ExtractLinks(),
                Images = ExtractImages(),
                Forms = ExtractForms(),
                Scripts = ExtractScripts(),
                Metadata = ExtractMetadata()
            };

            Console.WriteLine($"Content extracted successfully! Text length: {content.Text.Length} characters");
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Content extraction failed: {ex.Message}");
            throw;
        }
    }

    private string ExtractTextContent()
    {
        try
        {
            // Get visible text content, excluding script and style tags
            var textContent = ((IJavaScriptExecutor)driver).ExecuteScript(@"
                function getVisibleText() {
                    var walker = document.createTreeWalker(
                        document.body,
                        NodeFilter.SHOW_TEXT,
                        {
                            acceptNode: function(node) {
                                var parent = node.parentElement;
                                if (!parent) return NodeFilter.FILTER_REJECT;
                                
                                var style = window.getComputedStyle(parent);
                                if (style.display === 'none' || 
                                    style.visibility === 'hidden' ||
                                    parent.tagName === 'SCRIPT' ||
                                    parent.tagName === 'STYLE' ||
                                    parent.tagName === 'NOSCRIPT') {
                                    return NodeFilter.FILTER_REJECT;
                                }
                                
                                return NodeFilter.FILTER_ACCEPT;
                            }
                        }
                    );
                    
                    var text = '';
                    var node;
                    while (node = walker.nextNode()) {
                        text += node.textContent + ' ';
                    }
                    
                    return text.trim().replace(/\s+/g, ' ');
                }
                return getVisibleText();
            ").ToString();

            return textContent ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Text extraction failed: {ex.Message}");
            // Fallback to simple text extraction
            try
            {
                return driver.FindElement(By.TagName("body")).Text;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    private List<LinkInfo> ExtractLinks()
    {
        var links = new List<LinkInfo>();
        try
        {
            var linkElements = driver.FindElements(By.TagName("a"));
            foreach (var link in linkElements)
            {
                var href = link.GetAttribute("href");
                var text = link.Text?.Trim();
                var title = link.GetAttribute("title");

                if (!string.IsNullOrEmpty(href))
                {
                    links.Add(new LinkInfo
                    {
                        Url = href,
                        Text = text ?? string.Empty,
                        Title = title ?? string.Empty
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Link extraction failed: {ex.Message}");
        }
        return links;
    }

    private List<ImageInfo> ExtractImages()
    {
        var images = new List<ImageInfo>();
        try
        {
            var imgElements = driver.FindElements(By.TagName("img"));
            foreach (var img in imgElements)
            {
                var src = img.GetAttribute("src");
                var alt = img.GetAttribute("alt");
                var title = img.GetAttribute("title");

                if (!string.IsNullOrEmpty(src))
                {
                    images.Add(new ImageInfo
                    {
                        Src = src,
                        Alt = alt ?? string.Empty,
                        Title = title ?? string.Empty
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Image extraction failed: {ex.Message}");
        }
        return images;
    }

    private List<FormInfo> ExtractForms()
    {
        var forms = new List<FormInfo>();
        try
        {
            var formElements = driver.FindElements(By.TagName("form"));
            foreach (var form in formElements)
            {
                var action = form.GetAttribute("action");
                var method = form.GetAttribute("method") ?? "GET";
                var inputs = new List<string>();

                var inputElements = form.FindElements(By.TagName("input"));
                foreach (var input in inputElements)
                {
                    var name = input.GetAttribute("name");
                    var type = input.GetAttribute("type") ?? "text";
                    if (!string.IsNullOrEmpty(name))
                    {
                        inputs.Add($"{name} ({type})");
                    }
                }

                forms.Add(new FormInfo
                {
                    Action = action ?? string.Empty,
                    Method = method.ToUpper(),
                    Inputs = inputs
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Form extraction failed: {ex.Message}");
        }
        return forms;
    }

    private List<string> ExtractScripts()
    {
        var scripts = new List<string>();
        try
        {
            var scriptElements = driver.FindElements(By.TagName("script"));
            foreach (var script in scriptElements)
            {
                var src = script.GetAttribute("src");
                if (!string.IsNullOrEmpty(src))
                {
                    scripts.Add(src);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Script extraction failed: {ex.Message}");
        }
        return scripts;
    }

    private Dictionary<string, string> ExtractMetadata()
    {
        var metadata = new Dictionary<string, string>();
        try
        {
            // Extract meta tags
            var metaElements = driver.FindElements(By.TagName("meta"));
            foreach (var meta in metaElements)
            {
                var name = meta.GetAttribute("name") ?? meta.GetAttribute("property");
                var content = meta.GetAttribute("content");

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content))
                {
                    metadata[name] = content;
                }
            }

            // Add title
            metadata["title"] = driver.Title ?? string.Empty;

            // Add canonical URL if exists
            try
            {
                var canonical = driver.FindElement(By.CssSelector("link[rel='canonical']"));
                metadata["canonical"] = canonical.GetAttribute("href") ?? string.Empty;
            }
            catch { }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Metadata extraction failed: {ex.Message}");
        }
        return metadata;
    }

    public async Task<string> ExtractSpecificContent(string cssSelector)
    {
        try
        {
            var element = await WaitForElement(cssSelector);
            return element.Text;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Specific content extraction failed for '{cssSelector}': {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<List<string>> ExtractMultipleElements(string cssSelector)
    {
        var results = new List<string>();
        try
        {
            await HumanDelay(500, 1000);
            var elements = driver.FindElements(By.CssSelector(cssSelector));

            foreach (var element in elements)
            {
                var text = element.Text?.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    results.Add(text);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Multiple elements extraction failed for '{cssSelector}': {ex.Message}");
        }
        return results;
    }

    public void Dispose()
    {
        driver?.Quit();
        driver?.Dispose();
    }
}

// Data classes for structured content
public class PageContent
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<LinkInfo> Links { get; set; } = new List<LinkInfo>();
    public List<ImageInfo> Images { get; set; } = new List<ImageInfo>();
    public List<FormInfo> Forms { get; set; } = new List<FormInfo>();
    public List<string> Scripts { get; set; } = new List<string>();
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

public class LinkInfo
{
    public string Url { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class ImageInfo
{
    public string Src { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class FormInfo
{
    public string Action { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public List<string> Inputs { get; set; } = new List<string>();
}