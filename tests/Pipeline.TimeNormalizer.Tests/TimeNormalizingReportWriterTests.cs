// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer.Tests;

using System.IO.Abstractions;
using FileSystem.TestSupport;
using HtmlLog;
using Xunit;

public sealed class TimeNormalizingReportWriterTests
{
    private readonly IFileSystem fileSystem;
    private readonly WideFormatWeatherDataCsvWriter wideFormatWeatherDataCsvWriter;
    private readonly string parsedWideFormatDirectory;
    private readonly string htmlReportPath;
    private readonly WideFormatWeatherDataCsvReader wideFormatWeatherDataCsvReader;
    private readonly TimeNormalizingReportWriter reportWriter;
    private readonly HtmlLogFileManager htmlLogFileManager;

    public TimeNormalizingReportWriterTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        var parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        this.parsedWideFormatDirectory = this.fileSystem.Path.Combine(
            parsedStageDirectory,
            WeatherCsvOutputPaths.WideFormatDirectoryName);
        this.fileSystem.Directory.CreateDirectory(this.parsedWideFormatDirectory);
        this.htmlReportPath = this.fileSystem.Path.Combine(parsedStageDirectory, "result.html");
        this.wideFormatWeatherDataCsvWriter = new WideFormatWeatherDataCsvWriter(
            this.fileSystem,
            new PlaceCsvFileNameResolver(this.fileSystem));
        this.wideFormatWeatherDataCsvReader = new WideFormatWeatherDataCsvReader(this.fileSystem);
        this.reportWriter = new TimeNormalizingReportWriter(
            new TimeNormalizingPlaceErrorCountsBuilder(),
            this.wideFormatWeatherDataCsvReader,
            new PlaceCsvFileNameResolver(this.fileSystem),
            this.fileSystem);
        this.htmlLogFileManager = new HtmlLogFileManager(this.fileSystem);
    }

    [Fact]
    public void WriteReport_RowCountComparisonShowsZeroDeltaWhenInputMatchesOutput()
    {
        var archiveDate = new DateTime(2003, 1, 1);
        var inputRows = new List<WeatherDataRow>
        {
            new(
                archiveDate.AddHours(0),
                WeatherCharacteristics.Clear,
                -12,
                315,
                2.0m,
                750,
                70),
            new(
                archiveDate.AddHours(3),
                WeatherCharacteristics.Clear,
                -13,
                315,
                2.0m,
                750,
                70),
        };

        this.wideFormatWeatherDataCsvWriter.WritePlaceRows(
            this.parsedWideFormatDirectory,
            "Kyiv.csv",
            inputRows,
            includePlaceColumn: true);

        var normalizedRowsByPlace = new Dictionary<string, List<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] = inputRows,
        };

        this.reportWriter.WriteReport(
            this.htmlLogFileManager,
            this.htmlReportPath,
            totalPlaces: 1,
            timeNormalizationSuccessfulCount: 2,
            timeNormalizationUnsuccessfulCount: 0,
            missingTimeEntriesCount: 0,
            totalTimeSeconds: 1.0,
            averageTimePerPlaceSeconds: 1.0,
            normalizedRowsByPlace,
            normalizedFileCountsByPlace: new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Kyiv"] = 1 },
            timeNormalizationCountsByPlace: new Dictionary<string, PlaceTimeNormalizationCounts>(StringComparer.OrdinalIgnoreCase)
            {
                ["Kyiv"] = new PlaceTimeNormalizationCounts
                {
                    Successful = 2,
                    Unsuccessful = 0,
                    MissingTimeEntries = 0,
                },
            },
            issueCollector: new TimeNormalizationIssueCollector(),
            parsedWideFormatDirectory: this.parsedWideFormatDirectory);

        var html = this.fileSystem.File.ReadAllText(this.htmlReportPath);
        Assert.Contains("Row Count Comparison by Place", html, StringComparison.Ordinal);
        Assert.Contains("Wide Format Input Rows", html, StringComparison.Ordinal);
        Assert.Contains("Kyiv", html, StringComparison.Ordinal);
        Assert.Contains("0.00%", html, StringComparison.Ordinal);
    }
}
