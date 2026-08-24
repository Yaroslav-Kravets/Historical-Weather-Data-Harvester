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

public sealed class DateRangeClusterFormatter
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string YearMonthFormat = "yyyy-MM";
    private const string SameYearSeparator = ", ";
    private const string GroupSeparator = "; ";

    public string FormatDate(DateTime date) =>
        date.Date.ToString(DateFormat, CultureInfo.InvariantCulture);

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
        int? previousClusterYear = null;
        string? previousClusterYearMonth = null;
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

            this.AppendCompactCluster(
                builder,
                rangeStart,
                rangeEnd,
                ref previousClusterYear,
                ref previousClusterYearMonth);
            rangeStart = current;
            rangeEnd = current;
        }

        this.AppendCompactCluster(
            builder,
            rangeStart,
            rangeEnd,
            ref previousClusterYear,
            ref previousClusterYearMonth);
        return builder.ToString();
    }

    public string FormatRanges(IEnumerable<(DateTime Start, DateTime End)> ranges)
    {
        Argument.ThrowIfNull(ranges);

        var builder = new StringBuilder();
        int? previousClusterYear = null;
        string? previousClusterYearMonth = null;
        foreach (var (start, end) in ranges
            .Select(range =>
            {
                var startDate = range.Start.Date;
                var endDate = range.End.Date;

                if (startDate > endDate)
                {
                    throw new ArgumentException(
                        $"Range start ({startDate:yyyy-MM-dd}) must not be after end ({endDate:yyyy-MM-dd}).",
                        nameof(ranges));
                }

                return (Start: startDate, End: endDate);
            })
            .OrderBy(range => range.Start))
        {
            this.AppendCompactCluster(
                builder,
                start,
                end,
                ref previousClusterYear,
                ref previousClusterYearMonth);
        }

        return builder.ToString();
    }

    private void AppendCompactCluster(
        StringBuilder builder,
        DateTime rangeStart,
        DateTime rangeEnd,
        ref int? previousClusterYear,
        ref string? previousClusterYearMonth)
    {
        if (builder.Length > 0)
        {
            var separator = previousClusterYear != null
                && rangeStart.Year == previousClusterYear.Value
                ? SameYearSeparator
                : GroupSeparator;
            builder.Append(separator);
        }

        builder.Append(this.FormatCompactClusterStart(
            rangeStart,
            previousClusterYear,
            previousClusterYearMonth));
        if (rangeEnd != rangeStart)
        {
            builder.Append("..");
            builder.Append(this.FormatCompactRangeEnd(rangeStart, rangeEnd));
        }

        previousClusterYear = rangeStart.Year;
        previousClusterYearMonth = this.FormatYearMonthKey(rangeStart);
    }

    private string FormatCompactDate(DateTime date) => this.FormatDate(date);

    private string FormatYearMonthKey(DateTime date) =>
        date.Date.ToString(YearMonthFormat, CultureInfo.InvariantCulture);

    private string FormatCompactClusterStart(
        DateTime start,
        int? previousClusterYear,
        string? previousClusterYearMonth)
    {
        var yearMonth = this.FormatYearMonthKey(start);
        if (previousClusterYearMonth != null
            && string.Equals(yearMonth, previousClusterYearMonth, StringComparison.Ordinal))
        {
            return start.ToString("dd", CultureInfo.InvariantCulture);
        }

        if (previousClusterYear != null && start.Year == previousClusterYear.Value)
        {
            return start.ToString("MM-dd", CultureInfo.InvariantCulture);
        }

        return this.FormatCompactDate(start);
    }

    private string FormatCompactRangeEnd(DateTime start, DateTime end)
    {
        if (start.Year == end.Year && start.Month == end.Month)
        {
            return end.ToString("dd", CultureInfo.InvariantCulture);
        }

        if (start.Year == end.Year)
        {
            return end.ToString("MM-dd", CultureInfo.InvariantCulture);
        }

        return this.FormatCompactDate(end);
    }
}
