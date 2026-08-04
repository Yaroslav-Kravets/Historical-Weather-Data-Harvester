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
    private readonly DenormalizedWeatherDataCsvWriter denormalizedWeatherDataCsvWriter;
    private readonly string parsedStageDirectory;
    private readonly string htmlReportPath;
    private readonly DenormalizedWeatherDataCsvReader denormalizedWeatherDataCsvReader;
    private readonly TimeNormalizingReportWriter reportWriter;
    private readonly HtmlLogFileManager htmlLogFileManager;

    public TimeNormalizingReportWriterTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        this.parsedStageDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        this.fileSystem.Directory.CreateDirectory(this.parsedStageDirectory);
        this.htmlReportPath = this.fileSystem.Path.Combine(this.parsedStageDirectory, "result.html");
        this.denormalizedWeatherDataCsvWriter = new DenormalizedWeatherDataCsvWriter(
            this.fileSystem,
            new PlaceCsvFileNameResolver(this.fileSystem));
        this.denormalizedWeatherDataCsvReader = new DenormalizedWeatherDataCsvReader(this.fileSystem);
        this.reportWriter = new TimeNormalizingReportWriter(
            new TimeNormalizingPlaceErrorCountsBuilder(),
            this.denormalizedWeatherDataCsvReader,
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

        this.denormalizedWeatherDataCsvWriter.WritePlaceRows(
            this.parsedStageDirectory,
            "Kyiv.csv",
            inputRows,
            includePlaceColumn: true);

        var normalizedRowsByPlace = new Dictionary<string, List<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] = inputRows,
        };

        using (var htmlWriter = new HtmlLogWriter(this.htmlLogFileManager, this.htmlReportPath, "Test Report"))
        {
            this.reportWriter.WriteReport(
                htmlWriter,
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
                parsedStageDirectory: this.parsedStageDirectory);
        }

        var html = this.fileSystem.File.ReadAllText(this.htmlReportPath);
        Assert.Contains("Row Count Comparison by Place", html, StringComparison.Ordinal);
        Assert.Contains("Denormalized Input Rows", html, StringComparison.Ordinal);
        Assert.Contains("Kyiv", html, StringComparison.Ordinal);
        Assert.Contains("0.00%", html, StringComparison.Ordinal);
    }
}
