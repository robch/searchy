using Microsoft.Playwright;
using HtmlAgilityPack;

namespace PlaywrightWebScraper
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Default values
            string searchEngine = "google"; // Default to Google
            int maxResults = 10; // Default max results
            bool downloadContent = false;
            bool stripHtml = true;
            List<string> searchTerms = new List<string>();

            // Parse command-line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--google")
                {
                    searchEngine = "google";
                }
                else if (args[i] == "--bing")
                {
                    searchEngine = "bing";
                }
                else if (args[i] == "--content")
                {
                    downloadContent = true;
                }
                else if (args[i] == "--strip")
                {
                    stripHtml = true;
                }
                else if (args[i] == "--max" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int max))
                    {
                        maxResults = max;
                        i++; // Skip the next argument since it's the max value
                    }
                    else
                    {
                        Console.WriteLine("Invalid value for --max");
                        return;
                    }
                }
                else if (args[i].StartsWith("--"))
                {
                    // Unknown parameter
                    Console.WriteLine($"Unknown parameter: {args[i]}");
                }
                else
                {
                    searchTerms.Add(args[i]);
                }
            }

            // Build the search query
            string query = string.Join(" ", searchTerms);

            if (string.IsNullOrEmpty(query))
            {
                Console.WriteLine("No search terms provided.");
                return;
            }

            // Perform the search using Playwright
            await PerformSearchWithPlaywright(searchEngine, query, maxResults, downloadContent, stripHtml);
        }

        static async Task PerformSearchWithPlaywright(string searchEngine, string query, int maxResults, bool downloadContent, bool stripHtml)
        {
            // Initialize Playwright
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            // Determine the search URL based on the chosen search engine
            string searchUrl = searchEngine == "bing"
                ? $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}"
                : $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";

            // Navigate to the search engine
            await page.GotoAsync(searchUrl);

            // Extract search result URLs
            var urls = new List<string>();
            if (searchEngine == "google")
            {
                urls = await ExtractGoogleSearchResults(page, maxResults);
            }
            else if (searchEngine == "bing")
            {
                urls = await ExtractBingSearchResults(page, maxResults);
            }

            if (urls.Count == 0)
            {
                Console.WriteLine("No results found.");
                return;
            }

            if (!downloadContent)
            {
                foreach (var url in urls)
                {
                    Console.WriteLine(url);
                }
                return;
            }

            foreach (var url in urls)
            {
                Console.WriteLine("---separator---");
                Console.WriteLine($"url: {url}");
                Console.WriteLine("---separator---");
                string content = await FetchPageContent(page, url, stripHtml);
                Console.WriteLine(content);
            }

            Console.WriteLine("---separator---");
        }

        static async Task<List<string>> ExtractGoogleSearchResults(IPage page, int maxResults)
        {
            var urls = new List<string>();
            int currentPage = 1;

            while (urls.Count < maxResults)
            {
                // Extract URLs from the current page
                var elements = await page.QuerySelectorAllAsync("div#search a[href]");
                foreach (var element in elements)
                {
                    var href = await element.GetAttributeAsync("href");
                    if (href != null && href.StartsWith("http") && !href.Contains("google"))
                    {
                        if (!urls.Contains(href))
                        {
                            urls.Add(href);
                        }
                    }
                    if (urls.Count >= maxResults)
                    {
                        break;
                    }
                }

                if (urls.Count >= maxResults)
                {
                    break;
                }

                // Check for the "Next" button and navigate to the next page
                var nextButton = await page.QuerySelectorAsync("a#pnnext");
                if (nextButton != null)
                {
                    await nextButton.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    currentPage++;
                }
                else
                {
                    // No more pages
                    break;
                }
            }

            return urls.Take(maxResults).ToList();
        }

        static async Task<List<string>> ExtractBingSearchResults(IPage page, int maxResults)
        {
            var urls = new List<string>();
            int currentPage = 1;

            while (urls.Count < maxResults)
            {
                // Extract URLs from the current page
                var elements = await page.QuerySelectorAllAsync("li.b_algo a[href]");
                foreach (var element in elements)
                {
                    var href = await element.GetAttributeAsync("href");
                    if (href != null && href.StartsWith("http"))
                    {
                        if (!urls.Contains(href))
                        {
                            urls.Add(href);
                        }
                    }
                    if (urls.Count >= maxResults)
                    {
                        break;
                    }
                }

                if (urls.Count >= maxResults)
                {
                    break;
                }

                // Check for the "Next" button and navigate to the next page
                var nextButton = await page.QuerySelectorAsync("a.sb_pagN");
                if (nextButton != null)
                {
                    await nextButton.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    currentPage++;
                }
                else
                {
                    // No more pages
                    break;
                }
            }

            return urls.Take(maxResults).ToList();
        }

        static async Task<string> FetchPageContent(IPage page, string url, bool stripHtml)
        {
            try
            {
                // Navigate to the URL
                await page.GotoAsync(url);

                // Get the main content text
                var content = await GetContentWithRetries(page);

                if (content.Contains("Rate limit is exceeded. Try again in"))
                {
                    // Rate limit exceeded, wait and try again
                    var seconds = int.Parse(content.Split("Try again in ")[1].Split(" seconds.")[0]);
                    await Task.Delay(seconds * 1000);
                    return await FetchPageContent(page, url, stripHtml);
                }

                if (stripHtml)
                {
                    content = StripHtmlContent(content);
                }

                return content;
            }
            catch (Exception ex)
            {
                return $"Error fetching content from {url}: {ex.Message}\n{ex.StackTrace}";
            }
        }

        private static async Task<string> GetContentWithRetries(IPage page, int retries = 3)
        {
            var tryCount = retries + 1;
            while (true)
            {
                try
                {
                    var content = await page.ContentAsync();
                    return content;
                }
                catch (Exception ex)
                {
                    var rethrow = --tryCount == 0 || !ex.Message.Contains("navigating");
                    if (rethrow) throw;

                    await Task.Delay(1000);
                }
            }
        }

        static string StripHtmlContent(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.InnerText;
        }
    }
}
