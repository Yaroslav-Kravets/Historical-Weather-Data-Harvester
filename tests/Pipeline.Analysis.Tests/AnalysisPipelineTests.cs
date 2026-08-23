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
    public void AnalyzeStage_WritesCsvAndUsageTableBeforeFooter()
    {
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "parsed");
        var narrowFormatDirectory = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            WeatherCsvOutputPaths.NarrowFormatDirectoryName);
        this.WritePlaceCsv(
            narrowFormatDirectory,
            "Kyiv.csv",
            CreateRow(WeatherCharacteristics.Clear),
            CreateRow(WeatherCharacteristics.Rain));

        var htmlReportPath = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            "result-analysis2026-08-06_08-00-00.html");
        this.fileSystem.Directory.CreateDirectory(parsedStageDirectory);

        var pipeline = this.CreatePipeline();
        pipeline.AnalyzeStage(new AnalysisRunOptions(
            parsedStageDirectory,
            htmlReportPath));

        var coverageCsvPath = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            WeatherCsvOutputPaths.PlaceDateCoverageFileName);
        Assert.True(this.fileSystem.File.Exists(coverageCsvPath));

        var coverageCsv = this.fileSystem.File.ReadAllText(coverageCsvPath);
        Assert.Contains("Place,FirstDate,LastDate,SuccessfulDays,SkippedDays,SkippedDates", coverageCsv, StringComparison.Ordinal);
        Assert.Contains("Kyiv,2003-01-01,2003-01-01,1,0,", coverageCsv, StringComparison.Ordinal);

        var usageCsvPath = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName);
        Assert.True(this.fileSystem.File.Exists(usageCsvPath));

        var usageCsv = this.fileSystem.File.ReadAllText(usageCsvPath);
        Assert.Contains("EnglishName,NameInHtml,RowCount,PercentOfRows", usageCsv, StringComparison.Ordinal);
        Assert.Contains("Clear,ясно,1,50.00000%", usageCsv, StringComparison.Ordinal);
        Assert.Contains("Rain,дождь,1,50.00000%", usageCsv, StringComparison.Ordinal);

        Assert.True(this.fileSystem.File.Exists(htmlReportPath));
        var html = this.fileSystem.File.ReadAllText(htmlReportPath);
        Assert.Contains("Place Date Coverage", html, StringComparison.Ordinal);
        Assert.Contains("Weather Characteristics Usage", html, StringComparison.Ordinal);
        Assert.Contains("English Name", html, StringComparison.Ordinal);
        Assert.Contains("Clear", html, StringComparison.Ordinal);
        Assert.Contains("ясно", html, StringComparison.Ordinal);
        Assert.Contains("50.00000%", html, StringComparison.Ordinal);
        Assert.Contains("End of summary report", html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("Place Date Coverage", StringComparison.Ordinal)
            < html.IndexOf("Weather Characteristics Usage", StringComparison.Ordinal));
        Assert.True(
            html.IndexOf("Weather Characteristics Usage", StringComparison.Ordinal)
            < html.IndexOf("End of summary report", StringComparison.Ordinal));
    }

    [Fact]
    public void AnalyzeStage_WritesCoverageCsv_WithClusteredSkippedDates()
    {
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "parsed-gaps");
        var narrowFormatDirectory = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            WeatherCsvOutputPaths.NarrowFormatDirectoryName);
        this.WritePlaceCsv(
            narrowFormatDirectory,
            "Kyiv.csv",
            CreateRow(WeatherCharacteristics.Clear, new DateTime(2003, 1, 1)),
            CreateRow(WeatherCharacteristics.Clear, new DateTime(2003, 1, 3)),
            CreateRow(WeatherCharacteristics.Clear, new DateTime(2003, 1, 6)));

        var htmlReportPath = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            "result-analysis.html");
        this.fileSystem.Directory.CreateDirectory(parsedStageDirectory);

        this.CreatePipeline().AnalyzeStage(new AnalysisRunOptions(
            parsedStageDirectory,
            htmlReportPath));

        var coverageCsv = this.fileSystem.File.ReadAllText(
            this.fileSystem.Path.Combine(
                parsedStageDirectory,
                WeatherCsvOutputPaths.PlaceDateCoverageFileName));
        Assert.Contains(
            "Kyiv,2003-01-01,2003-01-06,3,3,\"2003-01-02,2003-01-04..2003-01-05\"",
            coverageCsv,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AnalyzeStage_WritesCoverageCsv_WithEmptyPlaceFile()
    {
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "parsed-mixed");
        var narrowFormatDirectory = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            WeatherCsvOutputPaths.NarrowFormatDirectoryName);
        this.WritePlaceCsv(
            narrowFormatDirectory,
            "Kyiv.csv",
            CreateRow(WeatherCharacteristics.Clear));
        this.WritePlaceCsv(narrowFormatDirectory, "Kharkiv.csv");

        var htmlReportPath = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            "result-analysis.html");
        this.fileSystem.Directory.CreateDirectory(parsedStageDirectory);

        this.CreatePipeline().AnalyzeStage(new AnalysisRunOptions(
            parsedStageDirectory,
            htmlReportPath));

        var coverageCsv = this.fileSystem.File.ReadAllText(
            this.fileSystem.Path.Combine(
                parsedStageDirectory,
                WeatherCsvOutputPaths.PlaceDateCoverageFileName));
        Assert.Contains("Kharkiv,,,0,0,", coverageCsv, StringComparison.Ordinal);
        Assert.Contains("Kyiv,2003-01-01,2003-01-01,1,0,", coverageCsv, StringComparison.Ordinal);
        Assert.True(
            coverageCsv.IndexOf("Kharkiv", StringComparison.Ordinal)
            < coverageCsv.IndexOf("Kyiv", StringComparison.Ordinal));
    }

    [Fact]
    public void AnalyzeStage_Throws_WhenNarrowFormatMissing()
    {
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "parsed");
        this.fileSystem.Directory.CreateDirectory(parsedStageDirectory);
        var htmlReportPath = this.fileSystem.Path.Combine(parsedStageDirectory, "result-analysis.html");
        var pipeline = this.CreatePipeline();

        var exception = Assert.Throws<DirectoryNotFoundException>(() =>
            pipeline.AnalyzeStage(new AnalysisRunOptions(
                parsedStageDirectory,
                htmlReportPath)));

        Assert.Contains("narrow-format", exception.Message, StringComparison.Ordinal);
        Assert.False(this.fileSystem.File.Exists(htmlReportPath));
    }

    [Fact]
    public void AnalyzeStage_Throws_WhenNoPlaceCsvs()
    {
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "parsed");
        var narrowFormatDirectory = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            WeatherCsvOutputPaths.NarrowFormatDirectoryName);
        this.fileSystem.Directory.CreateDirectory(narrowFormatDirectory);
        var htmlReportPath = this.fileSystem.Path.Combine(parsedStageDirectory, "result-analysis.html");
        var pipeline = this.CreatePipeline();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            pipeline.AnalyzeStage(new AnalysisRunOptions(
                parsedStageDirectory,
                htmlReportPath)));

        Assert.Contains("No place CSV files found", exception.Message, StringComparison.Ordinal);
        Assert.Contains(narrowFormatDirectory, exception.Message, StringComparison.Ordinal);
        Assert.False(this.fileSystem.File.Exists(htmlReportPath));
    }

    [Fact]
    public void AnalyzeStage_Throws_WhenPlaceCsvsHaveNoDataRows()
    {
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, "parsed");
        var narrowFormatDirectory = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            WeatherCsvOutputPaths.NarrowFormatDirectoryName);
        this.WritePlaceCsv(narrowFormatDirectory, "Kyiv.csv");
        var htmlReportPath = this.fileSystem.Path.Combine(parsedStageDirectory, "result-analysis.html");
        var pipeline = this.CreatePipeline();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            pipeline.AnalyzeStage(new AnalysisRunOptions(
                parsedStageDirectory,
                htmlReportPath)));

        Assert.Contains("No weather data rows found", exception.Message, StringComparison.Ordinal);
        Assert.Contains(narrowFormatDirectory, exception.Message, StringComparison.Ordinal);
        Assert.False(this.fileSystem.File.Exists(htmlReportPath));
    }

    private static WeatherDataRow CreateRow(
        WeatherCharacteristics characteristics,
        DateTime? time = null) =>
        new(time ?? new DateTime(2003, 1, 1, 0, 0, 0), characteristics, -5, 0, 1.0m, 750, 70);

    private AnalysisPipeline CreatePipeline()
    {
        var weatherCharacteristicConverter = new WeatherCharacteristicConverter();
        var weatherDataCsvRecordMap = new WeatherDataCsvRecordMap(
            new WeatherCharacteristicsEnglishCsvConverter(weatherCharacteristicConverter));
        return new AnalysisPipeline(
            NullLogger<AnalysisPipeline>.Instance,
            this.fileSystem,
            new HtmlLogFileManager(this.fileSystem),
            new NarrowFormatWeatherDataCsvReader(this.fileSystem, weatherDataCsvRecordMap),
            new PlaceDateCoverageAggregator(new DateRangeClusterFormatter()),
            new WeatherCharacteristicUsageAggregator(weatherCharacteristicConverter),
            new PlaceDateCoverageCsvWriter(
                NullLogger<PlaceDateCoverageCsvWriter>.Instance,
                this.fileSystem,
                new CsvRecordWriter(this.fileSystem)),
            new WeatherCharacteristicUsageCsvWriter(
                NullLogger<WeatherCharacteristicUsageCsvWriter>.Instance,
                this.fileSystem,
                new CsvRecordWriter(this.fileSystem)),
            new AnalysisReportWriter(
                NullLogger<AnalysisReportWriter>.Instance));
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
}
