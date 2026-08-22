// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv.Writers;

using Pipeline.Core.Tests.Csv.TestSupport;
using Xunit;

public sealed class DenormalizedWeatherDataCsvWriterTests
{
    private readonly CsvTestContext testContext;
    private readonly DenormalizedWeatherDataCsvWriter writer;
    private readonly string outputDirectory;

    public DenormalizedWeatherDataCsvWriterTests()
    {
        this.testContext = new CsvTestContext();
        this.outputDirectory = this.testContext.EnsureDirectoryUnderRoot("writer-output");
        this.writer = new DenormalizedWeatherDataCsvWriter(
            this.testContext.FileSystem,
            this.testContext.PlaceCsvFileNameResolver);
    }

    [Fact]
    public void WritePlaceRows_WritesPlaceColumnWhenRequested()
    {
        var rows = new[]
        {
            WeatherDataRowTestFactory.Create(
                new DateTime(2003, 1, 1, 0, 0, 0)),
        };

        this.writer.WritePlaceRows(this.outputDirectory, "Kyiv.csv", rows, includePlaceColumn: true);

        var csvRows = CsvTableAssertions.ReadRows(
            this.testContext.FileSystem,
            this.testContext.PathUnderRoot("writer-output", "Kyiv.csv"));
        var header = csvRows[0];
        var firstDataRow = csvRows[1];

        Assert.Equal(WeatherCsvColumns.Place, header[0]);
        Assert.Equal("Kyiv", CsvTableAssertions.GetRequiredValue(header, firstDataRow, WeatherCsvColumns.Place));
        Assert.Equal(
            "2003-01-01 00:00",
            CsvTableAssertions.GetRequiredValue(header, firstDataRow, WeatherCsvColumns.DateTime));
    }

    [Fact]
    public void WritePlaceRows_WritesScalarColumnsAndOneHotCharacteristicFlags()
    {
        var observationTime = new DateTime(2003, 1, 1, 0, 0, 0);
        var rows = new[]
        {
            WeatherDataRowTestFactory.Create(
                observationTime,
                WeatherCharacteristics.Clear | WeatherCharacteristics.LightSnow),
        };

        this.writer.WritePlaceRows(this.outputDirectory, "Kyiv.csv", rows);

        var csvRows = CsvTableAssertions.ReadRows(
            this.testContext.FileSystem,
            this.testContext.PathUnderRoot("writer-output", "Kyiv.csv"));
        var header = csvRows[0];
        var firstDataRow = csvRows[1];
        var clearColumn = WeatherCharacteristicsColumns.All
            .Single(pair => pair.Flag == WeatherCharacteristics.Clear)
            .ColumnName;
        var lightSnowColumn = WeatherCharacteristicsColumns.All
            .Single(pair => pair.Flag == WeatherCharacteristics.LightSnow)
            .ColumnName;
        var rainColumn = WeatherCharacteristicsColumns.All
            .Single(pair => pair.Flag == WeatherCharacteristics.Rain)
            .ColumnName;

        Assert.Equal(
            new[]
            {
                WeatherCsvColumns.DateTime,
                WeatherCsvColumns.Temperature,
                WeatherCsvColumns.WindDirection,
                WeatherCsvColumns.WindSpeed,
                WeatherCsvColumns.AtmosphericPressure,
                WeatherCsvColumns.Humidity,
            },
            header.Take(6).ToArray());

        Assert.Contains(clearColumn, header);
        Assert.Contains(lightSnowColumn, header);
        Assert.Equal("2003-01-01 00:00", CsvTableAssertions.GetRequiredValue(header, firstDataRow, WeatherCsvColumns.DateTime));
        Assert.Equal("-12", CsvTableAssertions.GetRequiredValue(header, firstDataRow, WeatherCsvColumns.Temperature));
        Assert.Equal("315", CsvTableAssertions.GetRequiredValue(header, firstDataRow, WeatherCsvColumns.WindDirection));
        Assert.Equal("2.0", CsvTableAssertions.GetRequiredValue(header, firstDataRow, WeatherCsvColumns.WindSpeed));
        Assert.Equal("750", CsvTableAssertions.GetRequiredValue(header, firstDataRow, WeatherCsvColumns.AtmosphericPressure));
        Assert.Equal("70", CsvTableAssertions.GetRequiredValue(header, firstDataRow, WeatherCsvColumns.Humidity));
        Assert.Equal("1", CsvTableAssertions.GetRequiredValue(header, firstDataRow, clearColumn));
        Assert.Equal("1", CsvTableAssertions.GetRequiredValue(header, firstDataRow, lightSnowColumn));
        Assert.Equal("0", CsvTableAssertions.GetRequiredValue(header, firstDataRow, rainColumn));
    }

    [Fact]
    public void WritePlaceRows_WritesAllZerosWhenCharacteristicsAreNone()
    {
        var rows = new[]
        {
            WeatherDataRowTestFactory.Create(
                new DateTime(2003, 1, 1, 3, 0, 0),
                WeatherCharacteristics.None,
                0,
                0,
                0m,
                760,
                80),
        };

        this.writer.WritePlaceRows(this.outputDirectory, "Odesa.csv", rows);

        var csvRows = CsvTableAssertions.ReadRows(
            this.testContext.FileSystem,
            this.testContext.PathUnderRoot("writer-output", "Odesa.csv"));
        var header = csvRows[0];
        var firstDataRow = csvRows[1];

        foreach (var characteristicColumnName in WeatherCharacteristicsColumns.All.Select(pair => pair.ColumnName))
        {
            Assert.Equal("0", CsvTableAssertions.GetRequiredValue(header, firstDataRow, characteristicColumnName));
        }
    }

    [Fact]
    public void WritePlaceRows_OmitsPlaceColumnByDefault()
    {
        var rows = new[]
        {
            WeatherDataRowTestFactory.Create(
                new DateTime(2003, 1, 1, 0, 0, 0)),
        };

        this.writer.WritePlaceRows(this.outputDirectory, "Kyiv.csv", rows);

        var csvRows = CsvTableAssertions.ReadRows(
            this.testContext.FileSystem,
            this.testContext.PathUnderRoot("writer-output", "Kyiv.csv"));
        var header = csvRows[0];

        Assert.Equal(WeatherCsvColumns.DateTime, header[0]);
        Assert.DoesNotContain(WeatherCsvColumns.Place, header);
    }
}
