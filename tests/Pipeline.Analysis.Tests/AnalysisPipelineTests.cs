// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis.Tests;

using System.IO.Abstractions;
using FileSystem.TestSupport;
using HtmlLog;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class AnalysisPipelineTests
{
    private readonly IFileSystem fileSystem = InMemoryFileSystem.Create();

    [Fact]
    public void Run_WritesCsvAndAppendsTableToParsedHtmlLog()
    {
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "parsed");
        var normalizedColumnsDirectory = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);
        this.WritePlaceCsv(
            normalizedColumnsDirectory,
            "Kyiv.csv",
            CreateRow(WeatherCharacteristics.Clear),
            CreateRow(WeatherCharacteristics.Rain));

        var runTimestamp = new DateTime(2026, 8, 6, 8, 0, 0);
        var htmlReportPath = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            "result2026-08-06_08-00-00.html");
        this.WriteMinimalHtmlReport(htmlReportPath, "Parsing");

        var pipeline = this.CreatePipeline();
        pipeline.Run(new AnalysisRunOptions(
            parsedStageDirectory,
            htmlReportPath,
            TimeNormalizedStageDirectory: null,
            TimeNormalizedHtmlReportPath: null,
            runTimestamp));

        var usageCsvPath = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName);
        Assert.True(this.fileSystem.File.Exists(usageCsvPath));

        var usageCsv = this.fileSystem.File.ReadAllText(usageCsvPath);
        Assert.Contains("EnglishName,NameInHtml,RowCount,PercentOfRows", usageCsv, StringComparison.Ordinal);
        Assert.Contains("Clear,ясно,1,50.00%", usageCsv, StringComparison.Ordinal);
        Assert.Contains("Rain,дождь,1,50.00%", usageCsv, StringComparison.Ordinal);

        var html = this.fileSystem.File.ReadAllText(htmlReportPath);
        Assert.Contains("Available Weather Characteristics", html, StringComparison.Ordinal);
        Assert.Contains("English Name", html, StringComparison.Ordinal);
        Assert.Contains("Clear", html, StringComparison.Ordinal);
        Assert.Contains("ясно", html, StringComparison.Ordinal);
        Assert.Contains("50.00%", html, StringComparison.Ordinal);
        Assert.Contains(HtmlLogWriter.FooterStartMarker, html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("Available Weather Characteristics", StringComparison.Ordinal)
            < html.IndexOf(HtmlLogWriter.FooterStartMarker, StringComparison.Ordinal));

        Assert.Empty(this.fileSystem.Directory.GetFiles(parsedStageDirectory, "weather-characteristics*.html"));
    }

    [Fact]
    public void Run_AnalyzesTimeNormalizedStage_WhenPresent()
    {
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "parsed");
        var timeNormalizedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "time-normalized");
        this.WritePlaceCsv(
            this.fileSystem.Path.Combine(parsedStageDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName),
            "Kyiv.csv",
            CreateRow(WeatherCharacteristics.Clear));
        this.WritePlaceCsv(
            this.fileSystem.Path.Combine(
                timeNormalizedStageDirectory,
                WeatherCsvOutputPaths.NormalizedColumnsDirectoryName),
            "Kyiv.csv",
            CreateRow(WeatherCharacteristics.Rain),
            CreateRow(WeatherCharacteristics.Rain));

        var parsedHtmlReportPath = this.fileSystem.Path.Combine(parsedStageDirectory, "result-parsed.html");
        var timeNormalizedHtmlReportPath = this.fileSystem.Path.Combine(
            timeNormalizedStageDirectory,
            "result-time-normalized.html");
        this.WriteMinimalHtmlReport(parsedHtmlReportPath, "Parsing");
        this.WriteMinimalHtmlReport(timeNormalizedHtmlReportPath, "Time Normalizing");

        var pipeline = this.CreatePipeline();
        pipeline.Run(new AnalysisRunOptions(
            parsedStageDirectory,
            parsedHtmlReportPath,
            timeNormalizedStageDirectory,
            timeNormalizedHtmlReportPath,
            new DateTime(2026, 8, 6, 9, 0, 0)));

        var parsedUsage = this.fileSystem.File.ReadAllText(
            this.fileSystem.Path.Combine(
                parsedStageDirectory,
                WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName));
        Assert.Contains("Clear,ясно,1,100.00%", parsedUsage, StringComparison.Ordinal);
        Assert.Contains("Available Weather Characteristics", this.fileSystem.File.ReadAllText(parsedHtmlReportPath), StringComparison.Ordinal);

        var timeNormalizedUsage = this.fileSystem.File.ReadAllText(
            this.fileSystem.Path.Combine(
                timeNormalizedStageDirectory,
                WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName));
        Assert.Contains("Rain,дождь,2,100.00%", timeNormalizedUsage, StringComparison.Ordinal);
        Assert.Contains(
            "Available Weather Characteristics",
            this.fileSystem.File.ReadAllText(timeNormalizedHtmlReportPath),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Run_SkipsMissingTimeNormalizedNormalizedColumns_WithWarningPath()
    {
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "parsed");
        var timeNormalizedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "time-normalized");
        this.fileSystem.Directory.CreateDirectory(timeNormalizedStageDirectory);
        this.WritePlaceCsv(
            this.fileSystem.Path.Combine(parsedStageDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName),
            "Kyiv.csv",
            CreateRow(WeatherCharacteristics.Clear));

        var parsedHtmlReportPath = this.fileSystem.Path.Combine(parsedStageDirectory, "result.html");
        this.WriteMinimalHtmlReport(parsedHtmlReportPath, "Parsing");

        var pipeline = this.CreatePipeline();
        pipeline.Run(new AnalysisRunOptions(
            parsedStageDirectory,
            parsedHtmlReportPath,
            timeNormalizedStageDirectory,
            this.fileSystem.Path.Combine(timeNormalizedStageDirectory, "result.html"),
            new DateTime(2026, 8, 6, 10, 0, 0)));

        Assert.True(this.fileSystem.File.Exists(
            this.fileSystem.Path.Combine(
                parsedStageDirectory,
                WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName)));
        Assert.False(this.fileSystem.File.Exists(
            this.fileSystem.Path.Combine(
                timeNormalizedStageDirectory,
                WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName)));
    }

    [Fact]
    public void Run_Throws_WhenParsedNormalizedColumnsMissing()
    {
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "parsed");
        this.fileSystem.Directory.CreateDirectory(parsedStageDirectory);
        var pipeline = this.CreatePipeline();

        var exception = Assert.Throws<DirectoryNotFoundException>(() =>
            pipeline.Run(new AnalysisRunOptions(
                parsedStageDirectory,
                this.fileSystem.Path.Combine(parsedStageDirectory, "result.html"),
                TimeNormalizedStageDirectory: null,
                TimeNormalizedHtmlReportPath: null,
                new DateTime(2026, 8, 6, 11, 0, 0))));

        Assert.Contains("normalized-columns", exception.Message, StringComparison.Ordinal);
    }

    private static WeatherDataRow CreateRow(WeatherCharacteristics characteristics) =>
        new(new DateTime(2003, 1, 1, 0, 0, 0), characteristics, -5, 0, 1.0m, 750, 70);

    private AnalysisPipeline CreatePipeline()
    {
        var weatherCharacteristicConverter = new WeatherCharacteristicConverter();
        var weatherDataCsvRecordMap = new WeatherDataCsvRecordMap(
            new WeatherCharacteristicsEnglishCsvConverter(weatherCharacteristicConverter));
        return new AnalysisPipeline(
            NullLogger<AnalysisPipeline>.Instance,
            this.fileSystem,
            new NormalizedColumnsWeatherDataCsvReader(this.fileSystem, weatherDataCsvRecordMap),
            new WeatherCharacteristicUsageAggregator(weatherCharacteristicConverter),
            new WeatherCharacteristicUsageCsvWriter(
                NullLogger<WeatherCharacteristicUsageCsvWriter>.Instance,
                this.fileSystem,
                new CsvRecordWriter(this.fileSystem)),
            new WeatherCharacteristicUsageReportWriter(
                NullLogger<WeatherCharacteristicUsageReportWriter>.Instance,
                this.fileSystem,
                new HtmlLogFileManager(this.fileSystem)));
    }

    private void WritePlaceCsv(string directory, string fileName, params WeatherDataRow[] rows)
    {
        this.fileSystem.Directory.CreateDirectory(directory);
        var weatherDataCsvRecordMap = new WeatherDataCsvRecordMap(
            new WeatherCharacteristicsEnglishCsvConverter(new WeatherCharacteristicConverter()));
        var records = rows.Select(WeatherDataCsvRecordMapper.ToRecord).ToList();
        new CsvRecordWriter(this.fileSystem).WriteRecords(
            directory,
            fileName,
            records,
            context => context.RegisterClassMap(weatherDataCsvRecordMap));
    }

    private void WriteMinimalHtmlReport(string reportPath, string title)
    {
        var directory = this.fileSystem.Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(directory))
        {
            this.fileSystem.Directory.CreateDirectory(directory);
        }

        using var fileManager = new HtmlLogFileManager(this.fileSystem);
        using (var htmlWriter = new HtmlLogWriter(fileManager, reportPath, title))
        {
            htmlWriter.WriteTable(
                new[] { new { Metric = "Placeholder", Value = "1" } },
                "Stage Placeholder");
        }
    }
}
