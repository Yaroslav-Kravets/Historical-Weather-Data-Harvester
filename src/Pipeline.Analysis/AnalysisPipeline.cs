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
    private readonly HtmlLogFileManager htmlLogFileManager;
    private readonly NarrowFormatWeatherDataCsvReader narrowFormatWeatherDataCsvReader;
    private readonly PlaceDateCoverageAggregator placeDateCoverageAggregator;
    private readonly WeatherCharacteristicUsageAggregator usageAggregator;
    private readonly PlaceDateCoverageCsvWriter placeDateCoverageCsvWriter;
    private readonly WeatherCharacteristicUsageCsvWriter usageCsvWriter;
    private readonly AnalysisReportWriter analysisReportWriter;

    public AnalysisPipeline(
        ILogger<AnalysisPipeline> logger,
        IFileSystem fileSystem,
        HtmlLogFileManager htmlLogFileManager,
        NarrowFormatWeatherDataCsvReader narrowFormatWeatherDataCsvReader,
        PlaceDateCoverageAggregator placeDateCoverageAggregator,
        WeatherCharacteristicUsageAggregator usageAggregator,
        PlaceDateCoverageCsvWriter placeDateCoverageCsvWriter,
        WeatherCharacteristicUsageCsvWriter usageCsvWriter,
        AnalysisReportWriter analysisReportWriter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(htmlLogFileManager);
        Argument.ThrowIfNull(narrowFormatWeatherDataCsvReader);
        Argument.ThrowIfNull(placeDateCoverageAggregator);
        Argument.ThrowIfNull(usageAggregator);
        Argument.ThrowIfNull(placeDateCoverageCsvWriter);
        Argument.ThrowIfNull(usageCsvWriter);
        Argument.ThrowIfNull(analysisReportWriter);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.htmlLogFileManager = htmlLogFileManager;
        this.narrowFormatWeatherDataCsvReader = narrowFormatWeatherDataCsvReader;
        this.placeDateCoverageAggregator = placeDateCoverageAggregator;
        this.usageAggregator = usageAggregator;
        this.placeDateCoverageCsvWriter = placeDateCoverageCsvWriter;
        this.usageCsvWriter = usageCsvWriter;
        this.analysisReportWriter = analysisReportWriter;
    }

    public void AnalyzeStage(AnalysisRunOptions options)
    {
        Argument.ThrowIfNull(options);
        Argument.ThrowIfNull(options.StageDirectory);
        Argument.ThrowIfNull(options.HtmlReportPath);

        this.logger.LogInformation("Start");

        var narrowFormatDirectory = this.fileSystem.Path.Combine(
            options.StageDirectory,
            WeatherCsvOutputPaths.NarrowFormatDirectoryName);

        if (!this.fileSystem.Directory.Exists(narrowFormatDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Weather CSV directory not found: {narrowFormatDirectory}");
        }

        var rowsByPlace = this.narrowFormatWeatherDataCsvReader.ReadAllPlaces(narrowFormatDirectory);
        if (rowsByPlace.Count == 0)
        {
            throw new InvalidOperationException(
                $"No place CSV files found in {narrowFormatDirectory}");
        }

        if (rowsByPlace.Values.Sum(rows => rows.Count) == 0)
        {
            throw new InvalidOperationException(
                $"No weather data rows found in {narrowFormatDirectory}");
        }

        this.logger.LogInformation(
            "Analyzing stage data for {StageDirectory} ({PlaceCount} places)",
            options.StageDirectory,
            rowsByPlace.Count);

        var coverageRows = this.placeDateCoverageAggregator.Aggregate(rowsByPlace);
        var usageRows = this.usageAggregator.Aggregate(rowsByPlace);
        this.placeDateCoverageCsvWriter.Write(coverageRows, options.StageDirectory);
        this.usageCsvWriter.Write(usageRows, options.StageDirectory);

        this.analysisReportWriter.Write(
            coverageRows,
            usageRows,
            this.htmlLogFileManager,
            options.HtmlReportPath);

        this.logger.LogInformation("Finish");
    }
}
