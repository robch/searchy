# searchy - the Playwright based web search tool

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
   cd src
   dotnet restore
   ```

3. Build the application:
   ```bash
   dotnet build
   ```

## Usage

See the usage information by running the application without any arguments:

```bash
cd bin
cd Debug
cd net8.0
searchy
```

```plaintext
Searchy - A simple web search and content retrieval tool using Playwright

USAGE: searchy get "URL" [...]
   OR: searchy search "TERMS" [...]

  COMMANDS:

    get              Downloads content from URL(s)
    search           Searches for content w/ Bing or Google

  OPTIONS:

    --headless       Run in headless mode (default: false)
    --strip          Strip HTML tags from downloaded content
    --save [FOLDER]  Save downloaded content to disk

  SEARCH OPTIONS:

    --bing           Use Bing search engine
    --google         Use Google search engine (default)
    --get            Download content from search results (default: false)
    --max NUMBER     Maximum number of search results (default: 10)
```

### Example

Search for "Playwright .NET" using Google, download content, and strip HTML:

```bash
searchy search "Playwright .NET" --google --get --strip
```

## Dependencies

- **Microsoft.Playwright**: Automates browser interactions.
- **HtmlAgilityPack**: Parses and manipulates HTML.

## Integration w/ Azure AI CLI

The application can be integrated with the [Azure AI CLI](https://github.com/Azure/azure-ai-cli) to perform web searches and analyze the content of search results using GenAI models, such as Azure OpenAI's GPT-4o.

1. Navigate to the `helper-functions` directory:
   ```bash
   cd integrations
   cd ai-cli
   cd helper-functions
   ```

2. Build the "helper-functions" assembly:
   ```bash
   dotnet build
   ```

3. Ensure that `searchy` is accessible via the PATH environment variable.

   On Linx/OSX:
   ```bash
   export PATH=$PATH:/path/to/searchy/src/bin/Debug/net8.0
   ```

   On Windows:
   ```bash
   set PATH=%PATH%;C:\path\to\searchy\src\bin\Debug\net8.0
   ```

4. Run the Azure AI CLI with the `--custom-helper-functions` option:

   ```bash
   ai chat --custom-helper-functions ./bin/Debug/net8.0/SearchyCliHelperFunctions.dll --interactive
   ```

5. Use the Azure AI CLI's interactive chat to ask about anything that may need to be searched.
   
   > Search for 'Playwright .NET', check a couple pages, and summarize what it does

## License

This project is licensed under the MIT License.
