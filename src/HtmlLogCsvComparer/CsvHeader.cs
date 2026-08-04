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

internal sealed class CsvHeader : IEquatable<CsvHeader>
{
    private static readonly CsvHeader EmptyInstance = new(Array.Empty<string>(), string.Empty, skipValidation: true);

    private readonly Dictionary<string, int> indexByName;

    public CsvHeader(IReadOnlyList<string> columns, string sourcePath)
        : this(columns, sourcePath, skipValidation: false)
    {
    }

    private CsvHeader(IReadOnlyList<string> columns, string sourcePath, bool skipValidation)
    {
        Argument.ThrowIfNull(columns);
        Argument.ThrowIfNull(sourcePath);

        var normalized = new string[columns.Count];
        for (var i = 0; i < columns.Count; i++)
        {
            normalized[i] = columns[i] ?? string.Empty;
        }

        this.Columns = normalized;
        this.indexByName = new Dictionary<string, int>();
        if (skipValidation)
        {
            return;
        }

        for (var i = 0; i < normalized.Length; i++)
        {
            if (!this.indexByName.TryAdd(normalized[i], i))
            {
                throw new CsvDataException(
                    $"duplicate column '{normalized[i]}' in '{sourcePath}'; cannot compare files with ambiguous columns.");
            }
        }
    }

    public static CsvHeader Empty => EmptyInstance;

    public IReadOnlyList<string> Columns { get; }

    public int Count => this.Columns.Count;

    public static bool operator ==(CsvHeader? left, CsvHeader? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(CsvHeader? left, CsvHeader? right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(CsvHeader? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
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
        return this.Equals(obj as CsvHeader);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var column in this.Columns)
        {
            hash.Add(column);
        }

        return hash.ToHashCode();
    }

    public CsvColumnIntersection Intersect(CsvHeader other)
    {
        Argument.ThrowIfNull(other);

        var leftOnly = new List<string>();
        var intersecting = new List<string>();
        var intersectingPairs = new List<(int LeftIndex, int RightIndex)>();
        foreach (var leftHeader in this.Columns)
        {
            if (other.indexByName.TryGetValue(leftHeader, out var rightIndex))
            {
                intersecting.Add(leftHeader);
                intersectingPairs.Add((this.indexByName[leftHeader], rightIndex));
            }
            else
            {
                leftOnly.Add(leftHeader);
            }
        }

        var rightOnly = new List<string>();
        foreach (var rightHeader in other.Columns)
        {
            if (!this.indexByName.ContainsKey(rightHeader))
            {
                rightOnly.Add(rightHeader);
            }
        }

        return new CsvColumnIntersection(leftOnly, rightOnly, intersecting, intersectingPairs);
    }
}
