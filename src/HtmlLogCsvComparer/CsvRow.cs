// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

using Common;

internal sealed class CsvRow : IEquatable<CsvRow>
{
    public CsvRow(IReadOnlyList<string> fields)
    {
        Argument.ThrowIfNull(fields);

        this.Fields = fields;
    }

    public IReadOnlyList<string> Fields { get; }

    public int Count => this.Fields.Count;

    public string this[int index] => this.Fields[index];

    public static bool operator ==(CsvRow? left, CsvRow? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(CsvRow? left, CsvRow? right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(CsvRow? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (this.Fields.Count != other.Fields.Count)
        {
            return false;
        }

        for (var i = 0; i < this.Fields.Count; i++)
        {
            if (!string.Equals(this.Fields[i], other.Fields[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return this.Equals(obj as CsvRow);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var field in this.Fields)
        {
            hash.Add(field);
        }

        return hash.ToHashCode();
    }

    public bool IntersectingFieldsEqual(
        CsvRow other,
        IReadOnlyList<(int LeftIndex, int RightIndex)> intersectingPairs)
    {
        Argument.ThrowIfNull(other);
        Argument.ThrowIfNull(intersectingPairs);

        foreach (var (leftIndex, rightIndex) in intersectingPairs)
        {
            var leftField = this.Fields[leftIndex];
            var rightField = other.Fields[rightIndex];
            if (!string.Equals(leftField, rightField))
            {
                return false;
            }
        }

        return true;
    }
}
