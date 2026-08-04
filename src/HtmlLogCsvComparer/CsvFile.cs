// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Common;
using CsvHelper;

internal sealed class CsvFile
{
    private string? contentHash;

    private CsvFile(
        string relativePath,
        string displayPath,
        long length,
        CsvHeader header,
        IReadOnlyList<CsvRow> rows)
    {
        this.RelativePath = relativePath;
        this.DisplayPath = displayPath;
        this.Length = length;
        this.Header = header;
        this.Rows = rows;
    }

    public string RelativePath { get; }

    public string DisplayPath { get; }

    public long Length { get; }

    public CsvHeader Header { get; }

    public IReadOnlyList<CsvRow> Rows { get; }

    public bool HasDataRows => this.Rows.Count > 0;

    public string ContentHash => this.contentHash ??= this.ComputeContentHash();

    public static CsvFile Load(string relativePath, string displayPath, long length, Stream stream)
    {
        Argument.ThrowIfNull(relativePath);
        Argument.ThrowIfNull(displayPath);
        Argument.ThrowIfNull(stream);

        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            if (!csv.Read())
            {
                return new CsvFile(relativePath, displayPath, length, CsvHeader.Empty, Array.Empty<CsvRow>());
            }

            var header = new CsvHeader(ReadCurrentRowFields(csv), relativePath);
            var rows = new List<CsvRow>();
            while (csv.Read())
            {
                var fields = ReadCurrentRowFields(csv);
                if (fields.Count != header.Count)
                {
                    throw new CsvDataException(
                        $"CSV file '{relativePath}' has a data row with {fields.Count} fields but {header.Count} header columns.");
                }

                rows.Add(new CsvRow(fields));
            }

            return new CsvFile(relativePath, displayPath, length, header, rows);
        }
        catch (CsvHelperException ex)
        {
            throw new CsvDataException($"CSV file '{relativePath}' is malformed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Streams the CSV once to capture header, row count, and content hash without retaining rows.
    /// </summary>
    /// <returns>Metadata for hash/signature matching without materializing row field lists.</returns>
    public static CsvFileMetadata LoadMetadata(string relativePath, string displayPath, long length, Stream stream)
    {
        Argument.ThrowIfNull(relativePath);
        Argument.ThrowIfNull(displayPath);
        Argument.ThrowIfNull(stream);

        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            if (!csv.Read())
            {
                CsvContentHashSerializer.AppendHeader(hasher, CsvHeader.Empty);
                CsvContentHashSerializer.AppendRowCount(hasher, 0);
                var emptyHash = Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
                return new CsvFileMetadata(
                    relativePath,
                    displayPath,
                    length,
                    CsvHeader.Empty,
                    rowCount: 0,
                    emptyHash);
            }

            var header = new CsvHeader(ReadCurrentRowFields(csv), relativePath);
            CsvContentHashSerializer.AppendHeader(hasher, header);
            var rowCount = 0;
            while (csv.Read())
            {
                var fields = ReadCurrentRowFields(csv);
                if (fields.Count != header.Count)
                {
                    throw new CsvDataException(
                        $"CSV file '{relativePath}' has a data row with {fields.Count} fields but {header.Count} header columns.");
                }

                CsvContentHashSerializer.AppendRowFields(hasher, fields);
                rowCount++;
            }

            CsvContentHashSerializer.AppendRowCount(hasher, rowCount);
            var contentHash = Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
            return new CsvFileMetadata(relativePath, displayPath, length, header, rowCount, contentHash);
        }
        catch (CsvHelperException ex)
        {
            throw new CsvDataException($"CSV file '{relativePath}' is malformed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Streams both CSVs and returns whether parsed content is identical.
    /// Throws when either side has no data rows or a malformed row.
    /// </summary>
    /// <returns><see langword="true"/> when headers and all data rows match; otherwise <see langword="false"/>.</returns>
    public static bool AreContentsEqualByStreaming(
        string leftRelativePath,
        Stream leftStream,
        string rightRelativePath,
        Stream rightStream) =>
        MatchByStreaming(
            leftRelativePath,
            leftLength: 0,
            leftStream,
            rightRelativePath,
            rightLength: 0,
            rightStream,
            CsvMatchKind.RelativePath).ContentIdentical;

    /// <summary>
    /// Streams both CSVs once and builds a <see cref="MatchedCsvPair"/> with the same
    /// positional row/column semantics as <see cref="MatchWith"/>, without retaining rows.
    /// </summary>
    /// <returns>The matched pair with optional detail stats when content differs.</returns>
    public static MatchedCsvPair MatchByStreaming(
        string leftRelativePath,
        long leftLength,
        Stream leftStream,
        string rightRelativePath,
        long rightLength,
        Stream rightStream,
        CsvMatchKind matchKind)
    {
        Argument.ThrowIfNull(leftRelativePath);
        Argument.ThrowIfNull(leftStream);
        Argument.ThrowIfNull(rightRelativePath);
        Argument.ThrowIfNull(rightStream);

        try
        {
            using var leftReader = new StreamReader(leftStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var rightReader = new StreamReader(rightStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var leftCsv = new CsvReader(leftReader, CultureInfo.InvariantCulture);
            using var rightCsv = new CsvReader(rightReader, CultureInfo.InvariantCulture);

            var leftHasHeader = leftCsv.Read();
            var rightHasHeader = rightCsv.Read();
            if (!leftHasHeader)
            {
                EnsureHasDataRows(leftRelativePath, dataRowCount: 0);
            }

            if (!rightHasHeader)
            {
                EnsureHasDataRows(rightRelativePath, dataRowCount: 0);
            }

            var leftHeader = new CsvHeader(ReadCurrentRowFields(leftCsv), leftRelativePath);
            var rightHeader = new CsvHeader(ReadCurrentRowFields(rightCsv), rightRelativePath);
            var intersection = leftHeader.Intersect(rightHeader);
            var leftRowCount = 0;
            var rightRowCount = 0;
            var equalRowCount = 0;
            var intersectionRowsEqual = true;

            while (true)
            {
                var leftRead = leftCsv.Read();
                var rightRead = rightCsv.Read();
                if (!leftRead && !rightRead)
                {
                    break;
                }

                if (leftRead)
                {
                    leftRowCount++;
                    var leftFields = ReadCurrentRowFields(leftCsv);
                    if (leftFields.Count != leftHeader.Count)
                    {
                        throw new CsvDataException(
                            $"CSV file '{leftRelativePath}' has a data row with {leftFields.Count} fields but {leftHeader.Count} header columns.");
                    }

                    if (rightRead)
                    {
                        rightRowCount++;
                        var rightFields = ReadCurrentRowFields(rightCsv);
                        if (rightFields.Count != rightHeader.Count)
                        {
                            throw new CsvDataException(
                                $"CSV file '{rightRelativePath}' has a data row with {rightFields.Count} fields but {rightHeader.Count} header columns.");
                        }

                        if (FieldsEqual(leftFields, rightFields))
                        {
                            equalRowCount++;
                        }

                        if (intersectionRowsEqual
                            && !IntersectingFieldsEqual(
                                leftFields,
                                rightFields,
                                intersection.IntersectingIndexPairs))
                        {
                            intersectionRowsEqual = false;
                        }
                    }
                    else
                    {
                        intersectionRowsEqual = false;
                        DrainRemainingRows(leftCsv, leftRelativePath, leftHeader, ref leftRowCount);
                        break;
                    }
                }
                else
                {
                    intersectionRowsEqual = false;
                    rightRowCount++;
                    var rightFields = ReadCurrentRowFields(rightCsv);
                    if (rightFields.Count != rightHeader.Count)
                    {
                        throw new CsvDataException(
                            $"CSV file '{rightRelativePath}' has a data row with {rightFields.Count} fields but {rightHeader.Count} header columns.");
                    }

                    DrainRemainingRows(rightCsv, rightRelativePath, rightHeader, ref rightRowCount);
                    break;
                }
            }

            EnsureHasDataRows(leftRelativePath, leftRowCount);
            EnsureHasDataRows(rightRelativePath, rightRowCount);

            if (leftRowCount != rightRowCount)
            {
                intersectionRowsEqual = false;
            }

            var contentIdentical = leftHeader.Equals(rightHeader)
                && leftRowCount == rightRowCount
                && equalRowCount == leftRowCount;

            CsvRowComparisonStats? rowComparison = null;
            CsvColumnComparison? columnComparison = null;
            if (!contentIdentical)
            {
                rowComparison = new CsvRowComparisonStats(leftRowCount, rightRowCount, equalRowCount);
                columnComparison = new CsvColumnComparison(
                    intersection.LeftOnlyColumns,
                    intersection.RightOnlyColumns,
                    intersection.IntersectingColumns,
                    intersectionRowsEqual);
            }

            return new MatchedCsvPair(
                leftRelativePath,
                rightRelativePath,
                leftLength,
                rightLength,
                matchKind,
                contentIdentical,
                rowComparison,
                columnComparison);
        }
        catch (CsvHelperException ex)
        {
            throw new CsvDataException(
                $"CSV comparison failed due to malformed input ('{leftRelativePath}' / '{rightRelativePath}'): {ex.Message}",
                ex);
        }
    }

    public CsvFileSignature CreateSignature(string fileName) =>
        new(fileName, this.Header.Columns, this.Rows.Count);

    /// <summary>
    /// Compares this file's rows and columns with <paramref name="other"/> in a single pass.
    /// </summary>
    /// <remarks>
    /// Row matching is strictly positional: full-row equality for the <c>equal</c> count and
    /// intersecting-column equality both compare <c>Rows[i]</c> to <c>other.Rows[i]</c>.
    /// An inserted or deleted row near the top shifts later rows, so the pair can report a
    /// near-zero <c>equal</c> count and fail PARTLY EQUAL even when overlapping data would
    /// match under a natural key (e.g. place + date + time). Key-based alignment is not used.
    /// </remarks>
    /// <returns>Row counts/equal matches and the shared-column comparison result.</returns>
    public (CsvRowComparisonStats Rows, CsvColumnComparison Columns) CompareContentWith(CsvFile other)
    {
        Argument.ThrowIfNull(other);

        var intersection = this.Header.Intersect(other.Header);
        var existingRowCount = this.Rows.Count;
        var destinationRowCount = other.Rows.Count;
        var equalRowCount = 0;
        var intersectionRowsEqual = existingRowCount == destinationRowCount;
        var sharedCount = Math.Min(existingRowCount, destinationRowCount);

        for (var i = 0; i < sharedCount; i++)
        {
            if (this.Rows[i].Equals(other.Rows[i]))
            {
                equalRowCount++;
            }

            if (intersectionRowsEqual
                && !this.Rows[i].IntersectingFieldsEqual(other.Rows[i], intersection.IntersectingIndexPairs))
            {
                intersectionRowsEqual = false;
            }
        }

        if (existingRowCount != destinationRowCount)
        {
            intersectionRowsEqual = false;
        }

        return (
            new CsvRowComparisonStats(existingRowCount, destinationRowCount, equalRowCount),
            new CsvColumnComparison(
                intersection.LeftOnlyColumns,
                intersection.RightOnlyColumns,
                intersection.IntersectingColumns,
                intersectionRowsEqual));
    }

    public MatchedCsvPair MatchWith(CsvFile other, CsvMatchKind matchKind)
    {
        Argument.ThrowIfNull(other);

        var (rowComparison, columnComparison) = this.CompareContentWith(other);
        var contentIdentical = this.Header.Equals(other.Header)
            && rowComparison.ExistingRowCount == rowComparison.DestinationRowCount
            && rowComparison.EqualRowCount == rowComparison.ExistingRowCount;

        return new MatchedCsvPair(
            this.RelativePath,
            other.RelativePath,
            this.Length,
            other.Length,
            matchKind,
            contentIdentical,
            contentIdentical ? null : rowComparison,
            contentIdentical ? null : columnComparison);
    }

    private static void EnsureHasDataRows(string relativePath, int dataRowCount)
    {
        if (dataRowCount == 0)
        {
            throw new CsvDataException(
                $"CSV file '{relativePath}' has no data rows.");
        }
    }

    private static void DrainRemainingRows(
        CsvReader csv,
        string relativePath,
        CsvHeader header,
        ref int rowCount)
    {
        while (csv.Read())
        {
            rowCount++;
            var fields = ReadCurrentRowFields(csv);
            if (fields.Count != header.Count)
            {
                throw new CsvDataException(
                    $"CSV file '{relativePath}' has a data row with {fields.Count} fields but {header.Count} header columns.");
            }
        }
    }

    private static bool FieldsEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IntersectingFieldsEqual(
        IReadOnlyList<string> leftFields,
        IReadOnlyList<string> rightFields,
        IReadOnlyList<(int LeftIndex, int RightIndex)> intersectingPairs)
    {
        foreach (var (leftIndex, rightIndex) in intersectingPairs)
        {
            if (!string.Equals(leftFields[leftIndex], rightFields[rightIndex], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<string> ReadCurrentRowFields(CsvReader csv)
    {
        var fields = new string[csv.Parser.Count];
        for (var i = 0; i < csv.Parser.Count; i++)
        {
            fields[i] = csv.GetField(i) ?? string.Empty;
        }

        return fields;
    }

    private string ComputeContentHash()
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        CsvContentHashSerializer.AppendParsedContent(hasher, this.Header, this.Rows);

        return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
    }
}
