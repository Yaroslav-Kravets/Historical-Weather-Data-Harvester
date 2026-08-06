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
using Microsoft.Extensions.Logging;

public sealed class AnalysisPipeline
{
    private readonly ILogger<AnalysisPipeline> logger;
    private readonly IFileSystem fileSystem;
    private readonly NormalizedColumnsWeatherDataCsvReader normalizedColumnsWeatherDataCsvReader;
    private readonly WeatherCharacteristicUsageAggregator usageAggregator;
    private readonly WeatherCharacteristicUsageCsvWriter usageCsvWriter;
    private readonly WeatherCharacteristicUsageReportWriter usageReportWriter;

    public AnalysisPipeline(
        ILogger<AnalysisPipeline> logger,
        IFileSystem fileSystem,
        NormalizedColumnsWeatherDataCsvReader normalizedColumnsWeatherDataCsvReader,
        WeatherCharacteristicUsageAggregator usageAggregator,
        WeatherCharacteristicUsageCsvWriter usageCsvWriter,
        WeatherCharacteristicUsageReportWriter usageReportWriter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(normalizedColumnsWeatherDataCsvReader);
        Argument.ThrowIfNull(usageAggregator);
        Argument.ThrowIfNull(usageCsvWriter);
        Argument.ThrowIfNull(usageReportWriter);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.normalizedColumnsWeatherDataCsvReader = normalizedColumnsWeatherDataCsvReader;
        this.usageAggregator = usageAggregator;
        this.usageCsvWriter = usageCsvWriter;
        this.usageReportWriter = usageReportWriter;
    }

    public void Run(AnalysisRunOptions options)
    {
        Argument.ThrowIfNull(options);
        Argument.ThrowIfNull(options.ParsedStageDirectory);
        Argument.ThrowIfNull(options.ParsedHtmlReportPath);

        if (!string.IsNullOrWhiteSpace(options.TimeNormalizedStageDirectory))
        {
            Argument.ThrowIf(
                options.TimeNormalizedHtmlReportPath,
                path => string.IsNullOrWhiteSpace(path),
                "TimeNormalizedHtmlReportPath is required when TimeNormalizedStageDirectory is set.",
                nameof(options.TimeNormalizedHtmlReportPath));
        }

        this.logger.LogInformation("Weather characteristics analysis stage start");

        this.AnalyzeStage(
            options.ParsedStageDirectory,
            options.ParsedHtmlReportPath,
            required: true);

        if (!string.IsNullOrWhiteSpace(options.TimeNormalizedStageDirectory))
        {
            this.AnalyzeStage(
                options.TimeNormalizedStageDirectory!,
                options.TimeNormalizedHtmlReportPath!,
                required: false);
        }

        this.logger.LogInformation("Weather characteristics analysis stage complete");
    }

    private void AnalyzeStage(string stageDirectory, string htmlReportPath, bool required)
    {
        var normalizedColumnsDirectory = this.fileSystem.Path.Combine(
            stageDirectory,
            WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);

        if (!this.fileSystem.Directory.Exists(normalizedColumnsDirectory))
        {
            if (required)
            {
                throw new DirectoryNotFoundException(
                    $"Weather CSV directory not found: {normalizedColumnsDirectory}");
            }

            this.logger.LogWarning(
                "Skipping weather characteristics analysis for {StageDirectory}; " +
                "normalized-columns directory not found: {NormalizedColumnsDirectory}",
                stageDirectory,
                normalizedColumnsDirectory);
            return;
        }

        var rowsByPlace = this.normalizedColumnsWeatherDataCsvReader.ReadAllPlaces(normalizedColumnsDirectory);
        if (rowsByPlace.Count == 0)
        {
            if (required)
            {
                throw new InvalidOperationException(
                    $"No place CSV files found in {normalizedColumnsDirectory}");
            }

            this.logger.LogWarning(
                "Skipping weather characteristics analysis for {StageDirectory}; " +
                "no place CSV files in {NormalizedColumnsDirectory}",
                stageDirectory,
                normalizedColumnsDirectory);
            return;
        }

        this.logger.LogInformation(
            "Analyzing weather characteristics for {StageDirectory} ({PlaceCount} places)",
            stageDirectory,
            rowsByPlace.Count);

        var usageRows = this.usageAggregator.Aggregate(rowsByPlace);
        this.usageCsvWriter.Write(usageRows, stageDirectory);
        this.usageReportWriter.Write(usageRows, htmlReportPath);
    }
}
