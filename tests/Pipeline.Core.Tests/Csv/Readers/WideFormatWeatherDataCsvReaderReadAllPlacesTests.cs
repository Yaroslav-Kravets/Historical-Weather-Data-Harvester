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

public sealed class WideFormatWeatherDataCsvReaderReadAllPlacesTests
{
    private readonly CsvTestContext testContext;
    private readonly WideFormatWeatherDataCsvWriter writer;
    private readonly WideFormatWeatherDataCsvReader reader;

    public WideFormatWeatherDataCsvReaderReadAllPlacesTests()
    {
        this.testContext = new CsvTestContext();
        this.writer = new WideFormatWeatherDataCsvWriter(
            this.testContext.FileSystem,
            this.testContext.PlaceCsvFileNameResolver);
        this.reader = new WideFormatWeatherDataCsvReader(this.testContext.FileSystem);
    }

    [Fact]
    public void ReadAllPlaces_ReadsFromWideFormatDirectory()
    {
        var wideFormatDirectory = this.testContext.EnsureDirectoryUnderRoot(
            WeatherCsvOutputPaths.ParsedStageDirectoryName,
            WeatherCsvOutputPaths.WideFormatDirectoryName);

        this.writer.WritePlaceRows(
            wideFormatDirectory,
            "Kyiv.csv",
            new[]
            {
                WeatherDataRowTestFactory.Create(
                    new DateTime(2003, 1, 1, 0, 0, 0),
                    WeatherCharacteristics.Rain,
                    -5,
                    90,
                    3.5m,
                    745,
                    85),
            });

        var places = this.reader.ReadAllPlaces(wideFormatDirectory);

        Assert.Single(places);
        Assert.True(places.ContainsKey("Kyiv"));
        var rows = places["Kyiv"].Values.SelectMany(byDate => byDate).ToList();
        var row = Assert.Single(rows);
        Assert.Equal(new DateTime(2003, 1, 1, 0, 0, 0), row.Time);
        Assert.Equal(WeatherCharacteristics.Rain, row.WeatherCharacteristics);
        Assert.Equal(-5, row.Temperature);
        Assert.Equal(90, row.WindDirectionAzimuth);
        Assert.Equal(3.5m, row.WindSpeed);
        Assert.Equal(745, row.AtmosphericPressure);
        Assert.Equal(85, row.Humidity);
    }

    [Fact]
    public void ReadAllPlaces_FindsUppercaseCsvExtension()
    {
        var wideFormatDirectory = this.testContext.EnsureDirectoryUnderRoot(
            WeatherCsvOutputPaths.ParsedStageDirectoryName,
            WeatherCsvOutputPaths.WideFormatDirectoryName);

        this.writer.WritePlaceRows(
            wideFormatDirectory,
            "Kyiv.CSV",
            new[]
            {
                WeatherDataRowTestFactory.Create(
                    new DateTime(2003, 1, 1, 0, 0, 0),
                    WeatherCharacteristics.Rain,
                    -5,
                    90,
                    3.5m,
                    745,
                    85),
            });

        var places = this.reader.ReadAllPlaces(wideFormatDirectory);

        Assert.Single(places);
        Assert.True(places.ContainsKey("Kyiv"));
    }

    [Fact]
    public void ReadAllPlaces_Throws_WhenPlaceNamesCollideIgnoringCase()
    {
        var wideFormatDirectory = this.testContext.EnsureDirectoryUnderRoot(
            WeatherCsvOutputPaths.ParsedStageDirectoryName,
            WeatherCsvOutputPaths.WideFormatDirectoryName);

        this.writer.WritePlaceRows(
            wideFormatDirectory,
            "Kyiv.csv",
            new[]
            {
                WeatherDataRowTestFactory.Create(
                    new DateTime(2003, 1, 1, 0, 0, 0),
                    WeatherCharacteristics.Clear,
                    -5,
                    90,
                    3.5m,
                    745,
                    85),
            });
        this.writer.WritePlaceRows(
            wideFormatDirectory,
            "kyiv.CSV",
            new[]
            {
                WeatherDataRowTestFactory.Create(
                    new DateTime(2003, 1, 1, 0, 0, 0),
                    WeatherCharacteristics.Rain,
                    -8,
                    90,
                    1.0m,
                    740,
                    75),
            });

        var exception = Assert.Throws<InvalidDataException>(() => this.reader.ReadAllPlaces(wideFormatDirectory));
        Assert.Contains("duplicate place CSV", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReadAllPlaces_ReadsWhenStageRootHasSidecarFiles()
    {
        var parsedStageDirectory = this.testContext.EnsureDirectoryUnderRoot(WeatherCsvOutputPaths.ParsedStageDirectoryName);
        var wideFormatDirectory = this.testContext.EnsureDirectoryUnderRoot(
            WeatherCsvOutputPaths.ParsedStageDirectoryName,
            WeatherCsvOutputPaths.WideFormatDirectoryName);

        this.testContext.FileSystem.File.WriteAllText(
            this.testContext.FileSystem.Path.Combine(parsedStageDirectory, WeatherCsvOutputPaths.ParsedPlacesManifestFileName),
            "EnglishName,NameInHtml\nKyiv,Киеве\n");
        this.testContext.FileSystem.File.WriteAllText(
            this.testContext.FileSystem.Path.Combine(
                parsedStageDirectory,
                WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName),
            "EnglishName,NameInHtml,RowCount,PercentOfRows\nClear,ясно,1,100.00%\n");
        this.writer.WritePlaceRows(
            wideFormatDirectory,
            "Kyiv.csv",
            new[]
            {
                WeatherDataRowTestFactory.Create(
                    new DateTime(2003, 1, 1, 0, 0, 0),
                    WeatherCharacteristics.Clear,
                    -5,
                    90,
                    3.5m,
                    745,
                    85),
            });

        var places = this.reader.ReadAllPlaces(wideFormatDirectory);

        Assert.Single(places);
        Assert.True(places.ContainsKey("Kyiv"));
    }

    [Fact]
    public void ReadAllPlaces_ThrowsWhenDirectoryMissing()
    {
        var wideFormatDirectory = this.testContext.PathUnderRoot("missing-parsed", WeatherCsvOutputPaths.WideFormatDirectoryName);

        Assert.Throws<DirectoryNotFoundException>(() => this.reader.ReadAllPlaces(wideFormatDirectory));
    }
}
