// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

public sealed class CsvFileSignature : IEquatable<CsvFileSignature>
{
    public CsvFileSignature(string fileName, IReadOnlyList<string> columns, int rowCount)
    {
        this.FileName = fileName;
        this.Columns = columns;
        this.RowCount = rowCount;
    }

    public string FileName { get; }

    public IReadOnlyList<string> Columns { get; }

    public int RowCount { get; }

    /// <inheritdoc/>
    public bool Equals(CsvFileSignature? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!string.Equals(this.FileName, other.FileName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (this.RowCount != other.RowCount)
        {
            return false;
        }

        if (this.Columns.Count != other.Columns.Count)
        {
            return false;
        }

        for (var i = 0; i < this.Columns.Count; i++)
        {
            if (!string.Equals(this.Columns[i], other.Columns[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return this.Equals(obj as CsvFileSignature);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(this.FileName, StringComparer.OrdinalIgnoreCase);
        hash.Add(this.RowCount);
        foreach (var column in this.Columns)
        {
            hash.Add(column);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.FileName}[{string.Join(',', this.Columns)};rows={this.RowCount}]";
    }
}
