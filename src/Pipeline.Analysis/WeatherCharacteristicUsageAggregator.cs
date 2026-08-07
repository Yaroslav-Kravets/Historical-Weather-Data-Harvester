// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis;

using Common;

public sealed class WeatherCharacteristicUsageAggregator
{
    private readonly WeatherCharacteristicConverter weatherCharacteristicConverter;

    public WeatherCharacteristicUsageAggregator(WeatherCharacteristicConverter weatherCharacteristicConverter)
    {
        Argument.ThrowIfNull(weatherCharacteristicConverter);

        this.weatherCharacteristicConverter = weatherCharacteristicConverter;
    }

    public IReadOnlyList<WeatherCharacteristicUsageRow> Aggregate(
        IReadOnlyDictionary<string, IReadOnlyList<WeatherDataRow>> rowsByPlace)
    {
        Argument.ThrowIfNull(rowsByPlace);

        var countByFlag = new Dictionary<WeatherCharacteristics, int>();
        var allPairs = this.weatherCharacteristicConverter.GetAllPairs();
        var totalDataRows = 0;

        foreach (var (_, rows) in rowsByPlace)
        {
            foreach (var row in rows)
            {
                totalDataRows++;
                foreach (var (_, flag) in allPairs)
                {
                    if ((row.WeatherCharacteristics & flag) == flag)
                    {
                        countByFlag[flag] = countByFlag.GetValueOrDefault(flag) + 1;
                    }
                }
            }
        }

        return allPairs
            .Select(pair =>
            {
                var rowCount = countByFlag.GetValueOrDefault(pair.Flag, 0);
                var percent = totalDataRows > 0
                    ? (rowCount / (double)totalDataRows) * 100.0
                    : 0.0;

                return new WeatherCharacteristicUsageRow(
                    EnumDisplayNameFormatter.ToDisplayName(pair.Flag),
                    pair.NameInHtml,
                    rowCount,
                    percent);
            })
            .OrderByDescending(row => row.PercentOfRows)
            .ThenBy(row => row.EnglishName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
