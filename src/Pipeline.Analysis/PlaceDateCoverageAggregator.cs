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

public sealed class PlaceDateCoverageAggregator
{
    private readonly DateRangeClusterFormatter dateRangeClusterFormatter;

    public PlaceDateCoverageAggregator(DateRangeClusterFormatter dateRangeClusterFormatter)
    {
        Argument.ThrowIfNull(dateRangeClusterFormatter);

        this.dateRangeClusterFormatter = dateRangeClusterFormatter;
    }

    public IReadOnlyList<PlaceDateCoverageRow> Aggregate(
        IReadOnlyDictionary<string, IReadOnlyList<WeatherDataRow>> rowsByPlace)
    {
        Argument.ThrowIfNull(rowsByPlace);

        return rowsByPlace
            .Select(pair => this.BuildRow(pair.Key, pair.Value))
            .OrderBy(row => row.Place, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private PlaceDateCoverageRow BuildRow(string place, IReadOnlyList<WeatherDataRow> rows)
    {
        Argument.ThrowIfNull(rows);

        if (rows.Count == 0)
        {
            return new PlaceDateCoverageRow(place, null, null, 0, 0, string.Empty);
        }

        var dates = rows
            .Select(row => row.Time.Date)
            .Distinct()
            .OrderBy(date => date)
            .ToList();

        var firstDate = dates[0];
        var lastDate = dates[^1];
        var observedDays = dates.Count;
        var skippedDays = (lastDate - firstDate).Days + 1 - observedDays;
        var gapRanges = new List<(DateTime Start, DateTime End)>();

        for (var index = 0; index < dates.Count - 1; index++)
        {
            var gapStart = dates[index].AddDays(1);
            var gapEnd = dates[index + 1].AddDays(-1);
            if (gapStart <= gapEnd)
            {
                gapRanges.Add((gapStart, gapEnd));
            }
        }

        return new PlaceDateCoverageRow(
            place,
            this.dateRangeClusterFormatter.FormatDate(firstDate),
            this.dateRangeClusterFormatter.FormatDate(lastDate),
            observedDays,
            skippedDays,
            this.dateRangeClusterFormatter.FormatRanges(gapRanges));
    }
}
