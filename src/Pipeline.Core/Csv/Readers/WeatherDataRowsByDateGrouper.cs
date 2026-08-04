// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Readers;

public static class WeatherDataRowsByDateGrouper
{
    public static SortedDictionary<DateTime, List<WeatherDataRow>> Group(IEnumerable<WeatherDataRow> rows)
    {
        var rowsByDate = new SortedDictionary<DateTime, List<WeatherDataRow>>();
        foreach (var row in rows)
        {
            var date = row.Time.Date;
            if (!rowsByDate.TryGetValue(date, out var rowsForDate))
            {
                rowsForDate = new List<WeatherDataRow>();
                rowsByDate[date] = rowsForDate;
            }

            rowsForDate.Add(row);
        }

        return rowsByDate;
    }
}
