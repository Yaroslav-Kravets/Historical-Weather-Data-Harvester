// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv.Records;

using System.Globalization;
using System.Text;
using CsvHelper;
using Pipeline.Core.Tests.Csv.TestSupport;
using Xunit;

public sealed class WeatherDataCsvRecordMapTests
{
    private readonly CsvTestContext testContext;
    private readonly WeatherCharacteristicConverter weatherCharacteristicConverter;

    public WeatherDataCsvRecordMapTests()
    {
        this.testContext = new CsvTestContext();
        this.weatherCharacteristicConverter = new WeatherCharacteristicConverter();
    }

    [Fact]
    public void WriteAndRead_WithClassMap_RoundTripsEnglishDisplayNameCell()
    {
        var characteristics = WeatherCharacteristics.FreezingRain | WeatherCharacteristics.LightFog;
        var expectedCell = this.weatherCharacteristicConverter.ToEnglishCsvCell(characteristics);
        Assert.Equal("Freezing Rain, Light Fog", expectedCell);

        var record = new WeatherDataCsvRecord(
            WeatherDataRowTestFactory.Create(
                new DateTime(2003, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                characteristics));

        var csvText = this.WriteRecordsToString([record], registerClassMap: true);
        Assert.Contains(expectedCell, csvText, StringComparison.Ordinal);

        var roundTripped = this.ReadRecordsFromString(csvText, registerClassMap: true).Single();
        Assert.Equal(characteristics, roundTripped.WeatherCharacteristics);
    }

    [Fact]
    public void Write_WithoutClassMap_UsesEnumMemberNamesNotEnglishDisplayNames()
    {
        var characteristics = WeatherCharacteristics.FreezingRain;
        var englishCell = this.weatherCharacteristicConverter.ToEnglishCsvCell(characteristics);
        Assert.Equal("Freezing Rain", englishCell);

        var record = new WeatherDataCsvRecord(
            WeatherDataRowTestFactory.Create(
                new DateTime(2003, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                characteristics));

        var csvText = this.WriteRecordsToString([record], registerClassMap: false);

        Assert.DoesNotContain(englishCell, csvText, StringComparison.Ordinal);
        Assert.Contains("FreezingRain", csvText, StringComparison.Ordinal);
    }

    private string WriteRecordsToString(IEnumerable<WeatherDataCsvRecord> records, bool registerClassMap)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder, CultureInfo.InvariantCulture))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            if (registerClassMap)
            {
                csv.Context.RegisterClassMap(this.testContext.WeatherDataCsvRecordMap);
            }

            csv.WriteRecords(records);
        }

        return builder.ToString();
    }

    private List<WeatherDataCsvRecord> ReadRecordsFromString(string csvText, bool registerClassMap)
    {
        using var reader = new StringReader(csvText);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        if (registerClassMap)
        {
            csv.Context.RegisterClassMap(this.testContext.WeatherDataCsvRecordMap);
        }

        return csv.GetRecords<WeatherDataCsvRecord>().ToList();
    }
}
