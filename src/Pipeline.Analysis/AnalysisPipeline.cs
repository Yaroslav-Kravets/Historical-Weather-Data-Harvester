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

    public void AnalyzeStage(AnalysisRunOptions options)
    {
        Argument.ThrowIfNull(options);
        Argument.ThrowIfNull(options.StageDirectory);
        Argument.ThrowIfNull(options.HtmlWriter);

        var normalizedColumnsDirectory = this.fileSystem.Path.Combine(
            options.StageDirectory,
            WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);

        if (!this.fileSystem.Directory.Exists(normalizedColumnsDirectory))
        {
            if (options.Required)
            {
                throw new DirectoryNotFoundException(
                    $"Weather CSV directory not found: {normalizedColumnsDirectory}");
            }

            this.logger.LogWarning(
                "Skipping weather characteristics analysis for {StageDirectory}; " +
                "normalized-columns directory not found: {NormalizedColumnsDirectory}",
                options.StageDirectory,
                normalizedColumnsDirectory);
            return;
        }

        var rowsByPlace = this.normalizedColumnsWeatherDataCsvReader.ReadAllPlaces(normalizedColumnsDirectory);
        if (rowsByPlace.Count == 0)
        {
            if (options.Required)
            {
                throw new InvalidOperationException(
                    $"No place CSV files found in {normalizedColumnsDirectory}");
            }

            this.logger.LogWarning(
                "Skipping weather characteristics analysis for {StageDirectory}; " +
                "no place CSV files in {NormalizedColumnsDirectory}",
                options.StageDirectory,
                normalizedColumnsDirectory);
            return;
        }

        this.logger.LogInformation(
            "Analyzing weather characteristics for {StageDirectory} ({PlaceCount} places)",
            options.StageDirectory,
            rowsByPlace.Count);

        var usageRows = this.usageAggregator.Aggregate(rowsByPlace);
        this.usageCsvWriter.Write(usageRows, options.StageDirectory);
        this.usageReportWriter.Write(usageRows, options.HtmlWriter);
    }
}
