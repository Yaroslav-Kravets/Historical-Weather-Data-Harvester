// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv.Readers;

using Pipeline.Core.Tests.Csv.TestSupport;
using Xunit;

public sealed class DenormalizedWeatherDataCsvReaderReadPlaceFileTests
{
    private readonly CsvTestContext testContext;
    private readonly DenormalizedWeatherDataCsvReader reader;

    public DenormalizedWeatherDataCsvReaderReadPlaceFileTests()
    {
        this.testContext = new CsvTestContext();
        this.reader = new DenormalizedWeatherDataCsvReader(this.testContext.FileSystem);
    }

    [Fact]
    public void ReadPlaceFile_ReadsRowsFromExplicitDenormalizedCsv_WithPlaceColumnPresent()
    {
        var sourceRows = new[]
        {
            WeatherDataRowTestFactory.Create(
                new DateTime(2003, 1, 1, 0, 0, 0),
                WeatherCharacteristics.Clear,
                -12,
                315,
                2.0m,
                750,
                70),
        };

        var csvPath = this.testContext.PathUnderRoot("with-place", "Kyiv.csv");
        DenormalizedCsvFixtureWriter.WritePlaceFile(
            this.testContext.FileSystem,
            csvPath,
            sourceRows,
            includePlaceColumn: true);

        var rowsByDate = this.reader.ReadPlaceFile(csvPath);
        var row = rowsByDate.Values.SelectMany(dayRows => dayRows).Single();

        Assert.Equal(sourceRows[0].Time, row.Time);
        Assert.Equal(sourceRows[0].WeatherCharacteristics, row.WeatherCharacteristics);
        Assert.Equal(sourceRows[0].Temperature, row.Temperature);
        Assert.Equal(sourceRows[0].WindDirectionAzimuth, row.WindDirectionAzimuth);
        Assert.Equal(sourceRows[0].WindSpeed, row.WindSpeed);
        Assert.Equal(sourceRows[0].AtmosphericPressure, row.AtmosphericPressure);
        Assert.Equal(sourceRows[0].Humidity, row.Humidity);
    }

    [Fact]
    public void ReadPlaceFile_ReadsRowsFromExplicitDenormalizedCsv()
    {
        var sourceRows = new[]
        {
            WeatherDataRowTestFactory.Create(
                new DateTime(2003, 1, 1, 0, 0, 0),
                WeatherCharacteristics.Clear | WeatherCharacteristics.LightSnow,
                -12,
                315,
                2.0m,
                750,
                70),
            WeatherDataRowTestFactory.Create(
                new DateTime(2003, 1, 1, 3, 0, 0),
                WeatherCharacteristics.None,
                -10,
                180,
                1.5m,
                755,
                65),
        };

        var csvPath = this.testContext.PathUnderRoot("out", "Kyiv.csv");
        DenormalizedCsvFixtureWriter.WritePlaceFile(
            this.testContext.FileSystem,
            csvPath,
            sourceRows);

        var rowsByDate = this.reader.ReadPlaceFile(csvPath);
        var rows = rowsByDate.Values.SelectMany(dayRows => dayRows).OrderBy(row => row.Time).ToList();

        Assert.Equal(2, rows.Count);
        AssertRowEquals(sourceRows[0], rows[0]);
        AssertRowEquals(sourceRows[1], rows[1]);
    }

    [Fact]
    public void ReadPlaceFile_ReturnsSortedDictionaryWithChronologicalDateKeys()
    {
        var sourceRows = new[]
        {
            WeatherDataRowTestFactory.Create(new DateTime(2003, 1, 3, 12, 0, 0)),
            WeatherDataRowTestFactory.Create(new DateTime(2003, 1, 1, 0, 0, 0)),
            WeatherDataRowTestFactory.Create(new DateTime(2003, 1, 2, 6, 0, 0)),
            WeatherDataRowTestFactory.Create(new DateTime(2003, 1, 1, 3, 0, 0)),
        };

        var csvPath = this.testContext.PathUnderRoot("ordered", "Kyiv.csv");
        DenormalizedCsvFixtureWriter.WritePlaceFile(
            this.testContext.FileSystem,
            csvPath,
            sourceRows);

        var rowsByDate = this.reader.ReadPlaceFile(csvPath);

        Assert.IsType<SortedDictionary<DateTime, IReadOnlyList<WeatherDataRow>>>(rowsByDate);
        Assert.Equal(
            new[]
            {
                new DateTime(2003, 1, 1),
                new DateTime(2003, 1, 2),
                new DateTime(2003, 1, 3),
            },
            rowsByDate.Keys.ToArray());
        Assert.Equal(
            new[]
            {
                new DateTime(2003, 1, 1, 0, 0, 0),
                new DateTime(2003, 1, 1, 3, 0, 0),
            },
            rowsByDate[new DateTime(2003, 1, 1)].Select(row => row.Time).ToArray());
    }

    [Fact]
    public void ReadPlaceFile_ReturnsEmptySortedDictionary_WhenFileHasNoRows()
    {
        var csvPath = this.testContext.PathUnderRoot("empty", "Kyiv.csv");
        this.testContext.FileSystem.Directory.CreateDirectory(
            this.testContext.FileSystem.Path.GetDirectoryName(csvPath)!);
        this.testContext.FileSystem.File.WriteAllText(csvPath, string.Empty);

        var rowsByDate = this.reader.ReadPlaceFile(csvPath);

        Assert.IsType<SortedDictionary<DateTime, IReadOnlyList<WeatherDataRow>>>(rowsByDate);
        Assert.Empty(rowsByDate);
    }

    private static void AssertRowEquals(WeatherDataRow expected, WeatherDataRow actual)
    {
        Assert.Equal(expected.Time, actual.Time);
        Assert.Equal(expected.WeatherCharacteristics, actual.WeatherCharacteristics);
        Assert.Equal(expected.Temperature, actual.Temperature);
        Assert.Equal(expected.WindDirectionAzimuth, actual.WindDirectionAzimuth);
        Assert.Equal(expected.WindSpeed, actual.WindSpeed);
        Assert.Equal(expected.AtmosphericPressure, actual.AtmosphericPressure);
        Assert.Equal(expected.Humidity, actual.Humidity);
    }
}
