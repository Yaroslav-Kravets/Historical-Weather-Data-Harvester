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
using System.Text;
using Common;

public sealed class DateRangeClusterFormatter : IDateRangeClusterFormatter
{
    private const string DateFormat = "yyyy-MM-dd";

    public string Format(IEnumerable<DateTime> dates)
    {
        Argument.ThrowIfNull(dates);

        var orderedDates = dates
            .Select(date => date.Date)
            .Distinct()
            .OrderBy(date => date)
            .ToList();

        if (orderedDates.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var rangeStart = orderedDates[0];
        var rangeEnd = rangeStart;

        for (var index = 1; index < orderedDates.Count; index++)
        {
            var current = orderedDates[index];
            if (current == rangeEnd.AddDays(1))
            {
                rangeEnd = current;
                continue;
            }

            AppendCluster(builder, rangeStart, rangeEnd);
            rangeStart = current;
            rangeEnd = current;
        }

        AppendCluster(builder, rangeStart, rangeEnd);
        return builder.ToString();
    }

    private static void AppendCluster(StringBuilder builder, DateTime rangeStart, DateTime rangeEnd)
    {
        if (builder.Length > 0)
        {
            builder.Append(',');
        }

        builder.Append(FormatDate(rangeStart));
        if (rangeEnd != rangeStart)
        {
            builder.Append("..");
            builder.Append(FormatDate(rangeEnd));
        }
    }

    private static string FormatDate(DateTime date) =>
        date.ToString(DateFormat, CultureInfo.InvariantCulture);
}
