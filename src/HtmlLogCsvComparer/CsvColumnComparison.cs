// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

public sealed class CsvColumnComparison
{
    public CsvColumnComparison(
        IReadOnlyList<string> leftOnlyColumns,
        IReadOnlyList<string> rightOnlyColumns,
        IReadOnlyList<string> intersectingColumns,
        bool intersectionRowsEqual)
    {
        this.LeftOnlyColumns = leftOnlyColumns;
        this.RightOnlyColumns = rightOnlyColumns;
        this.IntersectingColumns = intersectingColumns;
        this.IntersectionRowsEqual = intersectionRowsEqual;
    }

    public IReadOnlyList<string> LeftOnlyColumns { get; }

    public IReadOnlyList<string> RightOnlyColumns { get; }

    public IReadOnlyList<string> IntersectingColumns { get; }

    public bool IntersectionRowsEqual { get; }
}
