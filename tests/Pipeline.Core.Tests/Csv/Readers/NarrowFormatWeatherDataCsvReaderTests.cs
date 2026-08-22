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

public sealed class NormalizedColumnsWeatherDataCsvReaderTests
{
    private readonly CsvTestContext testContext;
    private readonly NormalizedColumnsWeatherDataCsvReader reader;
    private readonly string weatherDirectory;

    public NormalizedColumnsWeatherDataCsvReaderTests()
    {
        this.testContext = new CsvTestContext();
        this.weatherDirectory = this.testContext.EnsureDirectoryUnderRoot("normalized-input");
        this.reader = new NormalizedColumnsWeatherDataCsvReader(
            this.testContext.FileSystem,
            this.testContext.WeatherDataCsvRecordMap);
    }

    [Fact]
    public void ReadAllPlaces_ThrowsDirectoryNotFoundException_WhenDirectoryDoesNotExist()
    {
        var missingDirectory = this.testContext.PathUnderRoot("missing-input");
        Assert.Throws<DirectoryNotFoundException>(() => this.reader.ReadAllPlaces(missingDirectory));
    }

    [Fact]
    public void ReadPlaceFile_ReadsCoreColumnData()
    {
        var archiveDate = new DateTime(2003, 1, 1);
        var records = new[]
        {
            new WeatherDataCsvRecord(WeatherDataRowTestFactory.Create(archiveDate.AddHours(0), WeatherCharacteristics.Clear)),
            new WeatherDataCsvRecord(WeatherDataRowTestFactory.Create(archiveDate.AddHours(3), WeatherCharacteristics.Rain, -10, 180, 1.5m, 760, 80)),
        };
        this.testContext.WriteWeatherRecords(this.weatherDirectory, "Kyiv.csv", records);

        var rows = this.reader.ReadPlaceFile(this.testContext.PathUnderRoot("normalized-input", "Kyiv.csv"));

        Assert.Equal(2, rows.Count);
        Assert.Equal(archiveDate, rows[0].Time);
        Assert.Equal(-12, rows[0].Temperature);
        Assert.Equal(315, rows[0].WindDirectionAzimuth);
        Assert.Equal(2.0m, rows[0].WindSpeed);
        Assert.Equal(750, rows[0].AtmosphericPressure);
        Assert.Equal(70, rows[0].Humidity);
        Assert.Equal(WeatherCharacteristics.Rain, rows[1].WeatherCharacteristics);
    }

    [Fact]
    public void ReadAllPlaces_ReadsAllPlaceFiles()
    {
        var archiveDate = new DateTime(2003, 1, 1);
        this.testContext.WriteWeatherRecords(
            this.weatherDirectory,
            "Kyiv.csv",
            new[] { new WeatherDataCsvRecord(WeatherDataRowTestFactory.Create(archiveDate, WeatherCharacteristics.Clear)) });
        this.testContext.WriteWeatherRecords(
            this.weatherDirectory,
            "Kharkiv.csv",
            new[] { new WeatherDataCsvRecord(WeatherDataRowTestFactory.Create(archiveDate, WeatherCharacteristics.Rain, -8, 90, 1.0m, 740, 75)) });

        var rowsByPlace = this.reader.ReadAllPlaces(this.weatherDirectory);

        Assert.Equal(2, rowsByPlace.Count);
        var kyivRows = Assert.Single(rowsByPlace["Kyiv"]);
        Assert.Equal(-12, kyivRows.Temperature);
        Assert.Equal(WeatherCharacteristics.Clear, kyivRows.WeatherCharacteristics);

        var kharkivRows = Assert.Single(rowsByPlace["Kharkiv"]);
        Assert.Equal(-8, kharkivRows.Temperature);
        Assert.Equal(90, kharkivRows.WindDirectionAzimuth);
        Assert.Equal(1.0m, kharkivRows.WindSpeed);
        Assert.Equal(740, kharkivRows.AtmosphericPressure);
        Assert.Equal(75, kharkivRows.Humidity);
        Assert.Equal(WeatherCharacteristics.Rain, kharkivRows.WeatherCharacteristics);
    }

    [Fact]
    public void ReadAllPlaces_Throws_WhenPlaceNamesCollideIgnoringCase()
    {
        var archiveDate = new DateTime(2003, 1, 1);
        this.testContext.WriteWeatherRecords(
            this.weatherDirectory,
            "Kyiv.csv",
            new[] { new WeatherDataCsvRecord(WeatherDataRowTestFactory.Create(archiveDate, WeatherCharacteristics.Clear)) });
        this.testContext.WriteWeatherRecords(
            this.weatherDirectory,
            "kyiv.CSV",
            new[] { new WeatherDataCsvRecord(WeatherDataRowTestFactory.Create(archiveDate, WeatherCharacteristics.Rain, -8, 90, 1.0m, 740, 75)) });

        var exception = Assert.Throws<InvalidDataException>(() => this.reader.ReadAllPlaces(this.weatherDirectory));
        Assert.Contains("duplicate place CSV", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
