// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Metadata;

public static class WeatherScalarCsvColumns
{
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm";

    public const string DateTime = "DateTime";
    public const string Temperature = "Temperature";
    public const string WindDirection = "WindDirection";
    public const string WindSpeed = "WindSpeed";
    public const string AtmosphericPressure = "AtmosphericPressure";
    public const string Humidity = "Humidity";

    public static readonly IReadOnlyList<string> ScalarColumns = new[]
    {
        DateTime,
        Temperature,
        WindDirection,
        WindSpeed,
        AtmosphericPressure,
        Humidity,
    };
}
