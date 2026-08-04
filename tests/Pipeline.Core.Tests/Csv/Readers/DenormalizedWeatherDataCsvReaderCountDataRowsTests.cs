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

public sealed class DenormalizedWeatherDataCsvReaderCountDataRowsTests
{
    private readonly CsvTestContext testContext;
    private readonly DenormalizedWeatherDataCsvWriter writer;
    private readonly DenormalizedWeatherDataCsvReader reader;

    public DenormalizedWeatherDataCsvReaderCountDataRowsTests()
    {
        this.testContext = new CsvTestContext();
        this.writer = new DenormalizedWeatherDataCsvWriter(
            this.testContext.FileSystem,
            this.testContext.PlaceCsvFileNameResolver);
        this.reader = new DenormalizedWeatherDataCsvReader(this.testContext.FileSystem);
    }

    [Fact]
    public void CountDataRows_CountsDataRowsOnly()
    {
        var outputDirectory = this.testContext.EnsureDirectoryUnderRoot("count");
        this.writer.WritePlaceRows(
            outputDirectory,
            "Odesa.csv",
            new[]
            {
                WeatherDataRowTestFactory.Create(
                    new DateTime(2003, 1, 1, 0, 0, 0),
                    WeatherCharacteristics.Clear,
                    0,
                    0,
                    0m,
                    760,
                    80),
            });

        var count = this.reader.CountDataRows(this.testContext.PathUnderRoot("count", "Odesa.csv"));

        Assert.Equal(1, count);
    }

    [Fact]
    public void CountDataRows_ReturnsZeroForHeaderOnlyFile()
    {
        var outputDirectory = this.testContext.EnsureDirectoryUnderRoot("header-only");
        var csvPath = this.testContext.PathUnderRoot("header-only", "Odesa.csv");
        this.testContext.FileSystem.File.WriteAllText(csvPath, "DateTime,Temperature\n");

        var count = this.reader.CountDataRows(csvPath);

        Assert.Equal(0, count);
    }
}
