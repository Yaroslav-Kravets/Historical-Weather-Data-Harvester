// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv.Writers;

using System.Globalization;
using System.IO.Abstractions;
using CsvHelper;
using FileSystem.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class NormalizedColumnsWeatherDataCsvWriterTests
{
    private readonly IFileSystem fileSystem;
    private readonly string outputDirectory;
    private readonly NormalizedColumnsWeatherDataCsvWriter writer;

    public NormalizedColumnsWeatherDataCsvWriterTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        this.outputDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        this.fileSystem.Directory.CreateDirectory(this.outputDirectory);
        var csvRecordWriter = new CsvRecordWriter(this.fileSystem);
        var placeCsvFileNameResolver = new PlaceCsvFileNameResolver(this.fileSystem);
        var weatherDataCsvRecordMap = new WeatherDataCsvRecordMap(
            new WeatherCharacteristicsEnglishCsvConverter(new WeatherCharacteristicConverter()));
        this.writer = new NormalizedColumnsWeatherDataCsvWriter(
            NullLogger<NormalizedColumnsWeatherDataCsvWriter>.Instance,
            this.fileSystem,
            csvRecordWriter,
            placeCsvFileNameResolver,
            weatherDataCsvRecordMap);
    }

    [Fact]
    public void WritePlaceRows_WritesCoreColumnHeadersAndDataRow()
    {
        var observationTime = TestDate(2003, 1, 1, hour: 0);
        var rowsByPlace = ResultsFor(
            "Kyiv",
            Row(observationTime, WeatherCharacteristics.Clear, -12, 315, 2.0m, 750, 70));

        this.writer.WritePlaceRows(rowsByPlace, this.outputDirectory, "normalized");

        var rows = this.ReadCsv(this.CsvPath("Kyiv.csv"));
        Assert.Equal(WeatherCsvColumns.CoreColumns, rows[0]);
        Assert.Equal(
            new[] { "2003-01-01 00:00", "-12", "315", "2.0", "750", "70", "Clear" },
            rows[1]);
    }

    [Fact]
    public void WritePlaceRows_UsesEnglishDisplayNameForCsvFileName()
    {
        var rowsByPlace = ResultsFor(
            "Chervona Zirka",
            Row(TestDate(2003, 1, 1, hour: 3), WeatherCharacteristics.Rain, 5, 180, 1.5m, 760, 80));

        this.writer.WritePlaceRows(rowsByPlace, this.outputDirectory, "normalized");

        Assert.True(this.fileSystem.File.Exists(this.CsvPath("Chervona Zirka.csv")));
    }

    [Fact]
    public void WritePlaceRows_WritesSortedEnglishWeatherCharacteristics()
    {
        var rowsByPlace = ResultsFor(
            "Kyiv",
            Row(
                TestDate(2003, 1, 1, hour: 6),
                WeatherCharacteristics.Rain | WeatherCharacteristics.Clear,
                0,
                90,
                3.0m,
                755,
                65));

        this.writer.WritePlaceRows(rowsByPlace, this.outputDirectory, "normalized");

        var rows = this.ReadCsv(this.CsvPath("Kyiv.csv"));
        Assert.Equal("Clear, Rain", rows[1][6]);
    }

    [Fact]
    public void WritePlaceRows_DoesNotCreateOutputDirectoryWhenResultsAreEmpty()
    {
        var outputDirectory = this.fileSystem.Path.Combine(this.outputDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);

        this.writer.WritePlaceRows(
            new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase),
            outputDirectory,
            "parsed");

        Assert.False(this.fileSystem.Directory.Exists(outputDirectory));
    }

    private static DateTime TestDate(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Unspecified);

    private static WeatherDataRow Row(
        DateTime time,
        WeatherCharacteristics characteristics,
        int temperature,
        int windDirectionAzimuth,
        decimal windSpeed,
        int atmosphericPressure,
        int humidity) =>
        new(time, characteristics, temperature, windDirectionAzimuth, windSpeed, atmosphericPressure, humidity);

    private static Dictionary<string, IReadOnlyList<WeatherDataRow>> ResultsFor(
        string englishPlaceName,
        params WeatherDataRow[] rows) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [englishPlaceName] = rows,
        };

    private string CsvPath(string fileName) => this.fileSystem.Path.Combine(this.outputDirectory, fileName);

    private List<string[]> ReadCsv(string csvPath)
    {
        using var reader = new StreamReader(this.fileSystem.File.OpenRead(csvPath));
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<string[]>();
        while (csv.Read())
        {
            var row = new string[csv.Parser.Count];
            for (var i = 0; i < csv.Parser.Count; i++)
            {
                row[i] = csv.GetField(i) ?? string.Empty;
            }

            rows.Add(row);
        }

        return rows;
    }
}
