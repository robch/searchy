using Microsoft.Playwright;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Net;

namespace PlaywrightWebScraper
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            if (args.Length == 0 || (args[0] != "search" && args[0] != "get"))
            {
                return ShowUsage();
            }

            var verb = args[0];
            var commandArgs = args.Skip(1).ToArray();

            return verb switch
            {
                "search" => await HandleSearchCommand(commandArgs),
                "get" => await HandleGetCommand(commandArgs),
                _ => 1,
            };
        }

        private static int ShowUsage()
        {
            Console.WriteLine("Searchy - A simple web search and content retrieval tool using Playwright");
            Console.WriteLine();
            Console.WriteLine("USAGE: searchy get \"URL\" [...]");
            Console.WriteLine("   OR: searchy search \"TERMS\" [...]");
            Console.WriteLine();
            Console.WriteLine("  COMMANDS:");
            Console.WriteLine();
            Console.WriteLine("    get              Downloads content from URL(s)");
            Console.WriteLine("    search           Searches for content w/ Bing or Google");
            Console.WriteLine();
            Console.WriteLine("  OPTIONS:");
            Console.WriteLine();
            Console.WriteLine("    --strip          Strip HTML tags from downloaded content");
            Console.WriteLine("    --save [FOLDER]  Save downloaded content to disk");
            Console.WriteLine();
            Console.WriteLine("  SEARCH OPTIONS:");
            Console.WriteLine();
            Console.WriteLine("    --bing           Use Bing search engine");
            Console.WriteLine("    --google         Use Google search engine (default)");
            Console.WriteLine("    --get            Download content from search results (default: false)");
            Console.WriteLine("    --max NUMBER     Maximum number of search results (default: 10)");
            return 1;
        }

        private static async Task<int> HandleSearchCommand(string[] args)
        {
            // Default values
            var searchTerms = new List<string>();
            var searchEngine = "google"; // Default to Google
            var maxResults = 10; // Default max results
            var getContent = false;
            var stripHtml = true;
            string? saveToFolder = null;

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
                else if (args[i] == "--get")
                {
                    getContent = true;
                }
                else if (args[i] == "--strip")
                {
                    stripHtml = true;
                }
                else if (args[i] == "--save")
                {
                    getContent = true;
                    if (args.Length > i + 1 && !args[i + 1].StartsWith("--"))
                    {
                        saveToFolder = args[i + 1];
                        i++; // Skip the next argument since it's the folder
                    }
                    else
                    {
                        saveToFolder = Environment.CurrentDirectory;
                    }
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
                        return 1;
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
            var query = string.Join(" ", searchTerms);
            if (string.IsNullOrEmpty(query))
            {
                Console.WriteLine("No search terms provided.");
                return 1;
            }

            // Perform the search using Playwright
            await SearchResultsFromQuery(searchEngine, query, maxResults, getContent, stripHtml, saveToFolder);
            return 0;
        }

        private static async Task<int> HandleGetCommand(string[] args)
        {
            // Default values
            var urls = new List<string>();
            var stripHtml = false;
            string? saveToFolder = null;

            // Parse command-line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("http"))
                {
                    urls.Add(args[i]);
                }
                else if (args[i] == "-")
                {
                    var lines = ReadAllLinesFromStdin()
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l => l.Trim());
                    urls.AddRange(lines);
                }
                else if (args[i].StartsWith("@") && File.Exists(args[i].Substring(1)))
                {
                    var lines = File.ReadAllLines(args[i].Substring(1))
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l => l.Trim());
                    urls.AddRange(lines);
                }
                else if (args[i] == "--strip")
                {
                    stripHtml = true;
                }
                else if (args[i] == "--save")
                {
                    if (args.Length > i + 1 && !args[i + 1].StartsWith("--"))
                    {
                        saveToFolder = args[i + 1];
                        i++; // Skip the next argument since it's the folder
                    }
                    else
                    {
                        saveToFolder = Environment.CurrentDirectory;
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown parameter: {args[i]}");
                    return 1;
                }
            }

            // Check if any URLs were provided
            if (urls.Count == 0)
            {
                Console.WriteLine("No URLs provided.");
                return 1;
            }

            // Check for invalid URLs
            var badUrls = urls.Where(l => !l.StartsWith("http")).ToList();
            if (badUrls.Any())
            {
                Console.WriteLine(badUrls.Count == 1
                    ? $"Invalid URL: {badUrls[0]}"
                    : $"Invalid URLs:\n" + string.Join(Environment.NewLine, badUrls.Select(u => $"  {u}")));
                return 1;
            }

            // Get content from the URLs
            await GetPageContentFromURLs(urls, stripHtml, saveToFolder);
            return 0;
        }

        private static async Task SearchResultsFromQuery(string searchEngine, string query, int maxResults, bool getContent, bool stripHtml, string? saveToFolder)
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

            if (!getContent)
            {
                foreach (var url in urls)
                {
                    Console.WriteLine(url);
                }
                return;
            }

            await GetPageContentFromURLs(page, urls, stripHtml, saveToFolder);
        }

        private static async Task<List<string>> ExtractGoogleSearchResults(IPage page, int maxResults)
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

        private static async Task<List<string>> ExtractBingSearchResults(IPage page, int maxResults)
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

        private static async Task GetPageContentFromURLs(List<string> urls, bool stripHtml, string? saveToFolder)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await GetPageContentFromURLs(page, urls, stripHtml, saveToFolder);
        }

        private static async Task GetPageContentFromURLs(IPage page, List<string> urls, bool stripHtml, string? saveToFolder)
        {
            if (urls.Count == 1)
            {
                var url = urls[0];
                var content = await FetchPageContent(page, url, stripHtml, saveToFolder);
                Console.WriteLine(content);
                return;
            }

            foreach (var url in urls)
            {
                Console.WriteLine("---separator---");
                Console.WriteLine($"url: {url}");
                Console.WriteLine("---separator---");

                var content = await FetchPageContent(page, url, stripHtml, saveToFolder);
                Console.WriteLine(content);
            }

            Console.WriteLine("---separator---");
        }

        private static async Task<string> FetchPageContent(IPage page, string url, bool stripHtml, string? saveToFolder)
        {
            try
            {
                // Navigate to the URL
                await page.GotoAsync(url);

                // Get the main content text
                var content = await FetchPageContentWithRetries(page);

                if (content.Contains("Rate limit is exceeded. Try again in"))
                {
                    // Rate limit exceeded, wait and try again
                    var seconds = int.Parse(content.Split("Try again in ")[1].Split(" seconds.")[0]);
                    await Task.Delay(seconds * 1000);
                    return await FetchPageContent(page, url, stripHtml, saveToFolder);
                }

                if (stripHtml)
                {
                    content = StripHtmlContent(content);
                }

                if (!string.IsNullOrEmpty(saveToFolder))
                {
                    var fileName = GenerateUniqueFileNameFromUrl(url, saveToFolder);
                    File.WriteAllText(fileName, content);
                }

                return content;
            }
            catch (Exception ex)
            {
                return $"Error fetching content from {url}: {ex.Message}\n{ex.StackTrace}";
            }
        }

        private static async Task<string> FetchPageContentWithRetries(IPage page, int retries = 3)
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

        private static string StripHtmlContent(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var innerText = ReplaceHtmlEntities(doc.DocumentNode.InnerText);
            var innerTextLines = innerText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var lines = new List<string>();
            var blanks = new List<string>();
            foreach (var line in innerTextLines)
            {
                var trimmed = line.Trim(new[] { ' ', '\t', '\r', '\n', '\v', '\f', '\u00A0', '\u200B' });
                if (string.IsNullOrEmpty(trimmed))
                {
                    blanks.Add(line);
                    continue;
                }

                var addBlanks = Math.Min(blanks.Count, 1);
                if (addBlanks > 0)
                {
                    while (addBlanks-- > 0)
                    {
                        lines.Add(string.Empty);
                    }
                    blanks.Clear();
                }

                lines.Add(line);
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string ReplaceHtmlEntities(string textWithHtmlEntities)
        {
            return WebUtility.HtmlDecode(textWithHtmlEntities);
        }

        private static string GenerateUniqueFileNameFromUrl(string url, string saveToFolder)
        {
            EnsureDirectoryExists(saveToFolder);
            
            var uri = new Uri(url);
            var path = uri.Host + uri.AbsolutePath + uri.Query;

            var parts = path.Split(_invalidFileNameChars, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
            var check = Path.Combine(saveToFolder, string.Join("-", parts));
            if (!File.Exists(check)) return check;

            while (true)
            {
                var guidPart = Guid.NewGuid().ToString().Substring(0, 8);
                var linuxFileTimePart = DateTime.Now.ToFileTimeUtc().ToString();
                var tryThis = check + "-" + linuxFileTimePart + "-" + guidPart;
                if (!File.Exists(tryThis)) return tryThis;
            }
        }

        private static void EnsureDirectoryExists(string saveToFolder)
        {
            if (!Directory.Exists(saveToFolder))
            {
                Directory.CreateDirectory(saveToFolder);
            }
        }

        private static IEnumerable<string?> ReadAllLinesFromStdin()
        {
            while (true)
            {
                var line = Console.ReadLine();
                if (line == null) yield break;
                yield return line;
            }
        }

        private static char[] GetInvalidFileNameChars()
        {
            var invalidCharList = Path.GetInvalidFileNameChars().ToList();
            for (char c = (char)0; c < 128; c++)
            {
                if (!char.IsLetterOrDigit(c)) invalidCharList.Add(c);
            }
            return invalidCharList.Distinct().ToArray();
        }

        private static char[] _invalidFileNameChars = GetInvalidFileNameChars();
    }
}