// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis;

using System.Globalization;
using Common;

public sealed class PlaceDateCoverageAggregator
{
    public IReadOnlyList<PlaceDateCoverageRow> Aggregate(
        IReadOnlyDictionary<string, IReadOnlyList<WeatherDataRow>> rowsByPlace)
    {
        Argument.ThrowIfNull(rowsByPlace);

        return rowsByPlace
            .Select(pair => BuildRow(pair.Key, pair.Value))
            .OrderBy(row => row.Place, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static PlaceDateCoverageRow BuildRow(string place, IReadOnlyList<WeatherDataRow> rows)
    {
        if (rows.Count == 0)
        {
            return new PlaceDateCoverageRow(place, null, null, 0);
        }

        var dates = rows
            .Select(row => row.Time.Date)
            .Distinct()
            .OrderBy(date => date)
            .ToList();

        var firstDate = dates[0];
        var lastDate = dates[^1];
        var spanDays = (lastDate - firstDate).Days + 1;
        var skippedDays = spanDays - dates.Count;

        return new PlaceDateCoverageRow(
            place,
            firstDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            lastDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            skippedDays);
    }
}
