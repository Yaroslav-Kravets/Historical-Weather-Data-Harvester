// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

public sealed class CsvRowComparisonStats
{
    public CsvRowComparisonStats(int existingRowCount, int destinationRowCount, int equalRowCount)
    {
        this.ExistingRowCount = existingRowCount;
        this.DestinationRowCount = destinationRowCount;
        this.EqualRowCount = equalRowCount;
    }

    public int ExistingRowCount { get; }

    public int DestinationRowCount { get; }

    public int EqualRowCount { get; }
}
