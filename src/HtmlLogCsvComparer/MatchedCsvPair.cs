// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

public sealed class MatchedCsvPair
{
    public MatchedCsvPair(
        string leftRelativePath,
        string rightRelativePath,
        long leftByteCount,
        long rightByteCount,
        CsvMatchKind matchKind,
        bool contentIdentical,
        CsvRowComparisonStats? rowComparison,
        CsvColumnComparison? columnComparison)
    {
        this.LeftRelativePath = leftRelativePath;
        this.RightRelativePath = rightRelativePath;
        this.LeftByteCount = leftByteCount;
        this.RightByteCount = rightByteCount;
        this.MatchKind = matchKind;
        this.ContentIdentical = contentIdentical;
        this.RowComparison = rowComparison;
        this.ColumnComparison = columnComparison;
    }

    public string LeftRelativePath { get; }

    public string RightRelativePath { get; }

    public long LeftByteCount { get; }

    public long RightByteCount { get; }

    public CsvMatchKind MatchKind { get; }

    public bool ContentIdentical { get; }

    public CsvRowComparisonStats? RowComparison { get; }

    public CsvColumnComparison? ColumnComparison { get; }

    /// <summary>
    /// Gets a value indicating whether this content-different pair is PARTLY EQUAL:
    /// both sides have left- or right-only columns, at least one shared column, equal row
    /// counts, and positional intersecting-field equality on those shared columns
    /// (<c>Rows[i]</c> vs <c>Rows[i]</c>). “Shared columns match” is index-based, not keyed
    /// by place/date/time or any other natural key.
    /// </summary>
    public bool IsPartlyEqual
    {
        get
        {
            if (this.ContentIdentical || this.ColumnComparison is not { } columns || this.RowComparison is not { } rows)
            {
                return false;
            }

            if (columns.LeftOnlyColumns.Count == 0 && columns.RightOnlyColumns.Count == 0)
            {
                return false;
            }

            if (columns.IntersectingColumns.Count == 0)
            {
                return false;
            }

            return rows.ExistingRowCount == rows.DestinationRowCount
                && columns.IntersectionRowsEqual;
        }
    }
}
