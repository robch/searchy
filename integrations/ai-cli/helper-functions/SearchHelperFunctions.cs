using System.Diagnostics;
using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

public static class SearchHelperFunctions
{
    [HelperFunctionDescription("Performs a search using Bing and returns the URLs of the search results.")]
    public static string SearchBing(string query)
    {
        return ExecuteSearchCommand(query, "--bing");
    }

    [HelperFunctionDescription("Performs a search using Google and returns the URLs of the search results.")]
    public static string SearchGoogle(string query)
    {
        return ExecuteSearchCommand(query, "--google");
    }

    [HelperFunctionDescription("Downloads content from a specific URL and returns the text content after stripping HTML tags.")]
    public static string DownloadAndStripHTML(string url)
    {
        return ExecuteGetCommand(url, "--strip");
    }

    private static string ExecuteSearchCommand(string query, string engine)
    {
        return ExecuteCommand($"search \"{query}\" {engine}");
    }

    private static string ExecuteGetCommand(string url, string option)
    {
        return ExecuteCommand($"get \"{url}\" {option}");
    }

    private static string ExecuteCommand(string arguments)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "searchy.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                using (System.IO.StreamReader outputReader = process.StandardOutput)
                using (System.IO.StreamReader errorReader = process.StandardError)
                {
                    string output = outputReader.ReadToEnd();
                    string error = errorReader.ReadToEnd();
                    return !string.IsNullOrWhiteSpace(error)
                        ? $"{output}\nError: {error}"
                        : output;
                }
            }
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
