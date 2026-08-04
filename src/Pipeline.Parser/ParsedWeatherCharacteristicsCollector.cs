// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using Common;

public sealed class ParsedWeatherCharacteristicsCollector
{
    private readonly WeatherCharacteristicConverter weatherCharacteristicConverter;

    public ParsedWeatherCharacteristicsCollector(WeatherCharacteristicConverter weatherCharacteristicConverter)
    {
        Argument.ThrowIfNull(weatherCharacteristicConverter);

        this.weatherCharacteristicConverter = weatherCharacteristicConverter;
    }

    public List<(string EnglishName, string NameInHtml)> Collect(
        Dictionary<string, SortedDictionary<DateTime, ParsedDateEntry>> parseResultsByPlace)
    {
        Argument.ThrowIfNull(parseResultsByPlace);
        var observedFlags = WeatherCharacteristics.None;

        foreach (var (_, resultsByDate) in parseResultsByPlace)
        {
            foreach (var (_, entry) in resultsByDate)
            {
                foreach (var row in entry.Result.WeatherDataRows)
                {
                    observedFlags |= row.WeatherCharacteristics;
                }
            }
        }

        return this.weatherCharacteristicConverter.GetObservedPairs(observedFlags).ToList();
    }
}
