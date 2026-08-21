// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis;

using System.IO.Abstractions;
using Common;
using HtmlLog;
using Microsoft.Extensions.Logging;

public sealed class AnalysisPipeline
{
    private const string HtmlReportTitle = "Historical Weather Data Harvester — Weather Characteristics Usage";

    private readonly ILogger<AnalysisPipeline> logger;
    private readonly IFileSystem fileSystem;
    private readonly HtmlLogFileManager htmlLogFileManager;
    private readonly NormalizedColumnsWeatherDataCsvReader normalizedColumnsWeatherDataCsvReader;
    private readonly WeatherCharacteristicUsageAggregator usageAggregator;
    private readonly WeatherCharacteristicUsageCsvWriter usageCsvWriter;
    private readonly WeatherCharacteristicUsageReportWriter usageReportWriter;

    public AnalysisPipeline(
        ILogger<AnalysisPipeline> logger,
        IFileSystem fileSystem,
        HtmlLogFileManager htmlLogFileManager,
        NormalizedColumnsWeatherDataCsvReader normalizedColumnsWeatherDataCsvReader,
        WeatherCharacteristicUsageAggregator usageAggregator,
        WeatherCharacteristicUsageCsvWriter usageCsvWriter,
        WeatherCharacteristicUsageReportWriter usageReportWriter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(htmlLogFileManager);
        Argument.ThrowIfNull(normalizedColumnsWeatherDataCsvReader);
        Argument.ThrowIfNull(usageAggregator);
        Argument.ThrowIfNull(usageCsvWriter);
        Argument.ThrowIfNull(usageReportWriter);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.htmlLogFileManager = htmlLogFileManager;
        this.normalizedColumnsWeatherDataCsvReader = normalizedColumnsWeatherDataCsvReader;
        this.usageAggregator = usageAggregator;
        this.usageCsvWriter = usageCsvWriter;
        this.usageReportWriter = usageReportWriter;
    }

    public void AnalyzeStage(AnalysisRunOptions options)
    {
        Argument.ThrowIfNull(options);
        Argument.ThrowIfNull(options.StageDirectory);
        Argument.ThrowIfNull(options.HtmlReportPath);

        this.logger.LogInformation("Start");

        var normalizedColumnsDirectory = this.fileSystem.Path.Combine(
            options.StageDirectory,
            WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);

        if (!this.fileSystem.Directory.Exists(normalizedColumnsDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Weather CSV directory not found: {normalizedColumnsDirectory}");
        }

        var rowsByPlace = this.normalizedColumnsWeatherDataCsvReader.ReadAllPlaces(normalizedColumnsDirectory);
        if (rowsByPlace.Count == 0)
        {
            throw new InvalidOperationException(
                $"No place CSV files found in {normalizedColumnsDirectory}");
        }

        this.logger.LogInformation(
            "Analyzing weather characteristics for {StageDirectory} ({PlaceCount} places)",
            options.StageDirectory,
            rowsByPlace.Count);

        var usageRows = this.usageAggregator.Aggregate(rowsByPlace);
        this.usageCsvWriter.Write(usageRows, options.StageDirectory);

        using var htmlWriter = new HtmlLogWriter(
            this.htmlLogFileManager,
            options.HtmlReportPath,
            HtmlReportTitle);
        this.usageReportWriter.Write(usageRows, htmlWriter);

        this.logger.LogInformation("Finish");
    }
}
