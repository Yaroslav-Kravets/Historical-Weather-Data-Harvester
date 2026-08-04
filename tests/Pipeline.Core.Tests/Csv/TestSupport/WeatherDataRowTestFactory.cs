// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv.TestSupport;

internal static class WeatherDataRowTestFactory
{
    public static WeatherDataRow Create(
        DateTime time,
        WeatherCharacteristics characteristics = WeatherCharacteristics.Clear,
        int temperature = -12,
        int windDirection = 315,
        decimal windSpeed = 2.0m,
        int atmosphericPressure = 750,
        int humidity = 70)
    {
        return new WeatherDataRow(
            time,
            characteristics,
            temperature,
            windDirection,
            windSpeed,
            atmosphericPressure,
            humidity);
    }
}
