# HtmlScrapper

Console application that downloads historical weather HTML pages from meteo.ua.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (no Mono required)

## Configuration

```bash
cp src/HtmlScrapper/Options.config.example src/HtmlScrapper/Options.config
```

Edit `Options.config` and set `Directory` paths for your machine (use `/path/to/...` on Linux, `D:\...` on Windows).

## Build

From the repository root:

```bash
dotnet build Historical-Weather-Data-Harvester.sln -c Release
```

## Run

```bash
dotnet run --project src/HtmlScrapper/HtmlScrapper.csproj
```

Or run the built executable from `src/HtmlScrapper/bin/Release/net8.0/HtmlScrapper`.

Press Enter to stop the harvester.
