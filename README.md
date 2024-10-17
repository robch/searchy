# Playwright Web Scraper

## Overview

Searchy is a is a command-line tool that allows users to perform web searches using Google or Bing and optionally download the content of the search results. The application is built using C# and leverages the Playwright library to automate browser interactions and HtmlAgilityPack to process HTML content.

## Features

- **Search Engines**: Supports Google and Bing.
- **Customizable Results**: Specify the maximum number of search results.
- **Content Downloading**: Option to download and display the content of search result pages.
- **HTML Stripping**: Option to strip HTML tags from downloaded content.

## Requirements

- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- Internet connection for accessing search engines.

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/robch/searchy.git
   cd searchy
   ```

2. Restore the required NuGet packages:
   ```bash
   dotnet restore
   ```

## Usage

Run the application with the desired command-line arguments:

```bash
dotnet run -- [options] [search terms]
```

### Options

- `--google`: Use Google for searching (default).
- `--bing`: Use Bing for searching.
- `--content`: Download the content of search result pages.
- `--strip`: Strip HTML tags from downloaded content.
- `--max [number]`: Specify the maximum number of search results (default is 10).

### Example

Search for "Playwright .NET" using Google, download content, and strip HTML:

```bash
dotnet run -- --google --content --strip "Playwright .NET"
```

## Dependencies

- **Microsoft.Playwright**: Automates browser interactions.
- **HtmlAgilityPack**: Parses and manipulates HTML.

## License

This project is licensed under the MIT License.
