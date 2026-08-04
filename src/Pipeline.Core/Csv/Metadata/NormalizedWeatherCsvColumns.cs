// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Metadata;

public static class NormalizedWeatherCsvColumns
{
    public const string WeatherCharacteristics = "Weather Characteristics";

    public static readonly IReadOnlyList<string> CoreColumns = new[]
    {
        WeatherScalarCsvColumns.DateTime,
        WeatherScalarCsvColumns.Temperature,
        WeatherScalarCsvColumns.WindDirection,
        WeatherScalarCsvColumns.WindSpeed,
        WeatherScalarCsvColumns.AtmosphericPressure,
        WeatherScalarCsvColumns.Humidity,
        WeatherCharacteristics,
    };
}
