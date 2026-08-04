// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

using System.IO.Abstractions;
using Common;

public sealed class CsvTreeComparer
{
    private readonly IFileSystem fileSystem;

    public CsvTreeComparer(IFileSystem fileSystem)
    {
        Argument.ThrowIfNull(fileSystem);

        this.fileSystem = fileSystem;
    }

    public Dictionary<string, string> CollectCsvPaths(string root)
    {
        Argument.ThrowIfNull(root);

        var source = CsvTreeSource.Create(this.fileSystem, root);
        return source.Paths.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    public CsvTreeComparison CompareCsvTrees(string leftDir, string rightDir)
    {
        Argument.ThrowIfNull(leftDir);
        Argument.ThrowIfNull(rightDir);

        var leftSource = CsvTreeSource.Create(this.fileSystem, leftDir);
        var rightSource = CsvTreeSource.Create(this.fileSystem, rightDir);
        using var leftSession = leftSource.OpenReadSession();
        using var rightSession = rightSource.OpenReadSession();
        return this.CompareCsvTrees(leftSession, rightSession);
    }

    internal CsvTreeComparison CompareCsvTrees(CsvTreeReadSession left, CsvTreeReadSession right)
    {
        Argument.ThrowIfNull(left);
        Argument.ThrowIfNull(right);

        var matchedPairs = new List<MatchedCsvPair>();
        var unmatchedLeft = new HashSet<string>(left.RelativePaths, StringComparer.OrdinalIgnoreCase);
        var unmatchedRight = new HashSet<string>(right.RelativePaths, StringComparer.OrdinalIgnoreCase);

        this.MatchByRelativePath(left, right, unmatchedLeft, unmatchedRight, matchedPairs);

        if (unmatchedLeft.Count > 0 || unmatchedRight.Count > 0)
        {
            var leftMetadata = LoadUnmatchedMetadata(left, unmatchedLeft);
            var rightMetadata = LoadUnmatchedMetadata(right, unmatchedRight);
            this.EnsureMetadataHaveDataRows(leftMetadata);
            this.EnsureMetadataHaveDataRows(rightMetadata);

            this.MatchByFileHash(leftMetadata, rightMetadata, unmatchedLeft, unmatchedRight, matchedPairs);
            this.MatchByFileNameAndColumns(
                left,
                right,
                leftMetadata,
                rightMetadata,
                unmatchedLeft,
                unmatchedRight,
                matchedPairs);
        }

        var leftOnly = unmatchedLeft.Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var rightOnly = unmatchedRight.Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var orderedPairs = matchedPairs
            .OrderBy(static pair => pair.LeftRelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static pair => pair.RightRelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CsvTreeComparison(
            left.Path,
            right.Path,
            left.CsvCount,
            right.CsvCount,
            orderedPairs,
            leftOnly,
            rightOnly);
    }

    private static Dictionary<string, CsvFileMetadata> LoadUnmatchedMetadata(
        CsvTreeReadSession session,
        HashSet<string> relativePaths)
    {
        var files = new Dictionary<string, CsvFileMetadata>(StringComparer.OrdinalIgnoreCase);
        foreach (var relative in relativePaths.Order(StringComparer.OrdinalIgnoreCase))
        {
            files[relative] = session.LoadMetadata(relative);
        }

        return files;
    }

    private static string FormatDuplicateHashMessage(
        string hash,
        IReadOnlyList<HashEntry> leftEntries,
        IReadOnlyList<HashEntry> rightEntries)
    {
        var leftPaths = string.Join(", ", leftEntries.Select(static entry => entry.Relative));
        var rightPaths = string.Join(", ", rightEntries.Select(static entry => entry.Relative));
        return
            $"duplicate file-hash signature {hash}: " +
            $"left=[{leftPaths}] right=[{rightPaths}]";
    }

    private static string FormatDuplicateSignatureMessage(
        CsvFileSignature signature,
        IReadOnlyList<SignatureEntry> leftEntries,
        IReadOnlyList<SignatureEntry> rightEntries)
    {
        var leftPaths = string.Join(", ", leftEntries.Select(static entry => entry.Relative));
        var rightPaths = string.Join(", ", rightEntries.Select(static entry => entry.Relative));
        return
            $"duplicate file-name+columns+rows signature {signature.FileName} " +
            $"(cols={signature.Columns.Count}, rows={signature.RowCount}): " +
            $"left=[{leftPaths}] right=[{rightPaths}]";
    }

    private static void MatchHashPair(
        HashEntry leftEntry,
        HashEntry rightEntry,
        Dictionary<string, CsvFileMetadata> unmatchedLeftFiles,
        Dictionary<string, CsvFileMetadata> unmatchedRightFiles,
        HashSet<string> unmatchedLeft,
        HashSet<string> unmatchedRight,
        List<MatchedCsvPair> matchedPairs)
    {
        // Equal content hashes imply parsed-content identity; skip a second full scan.
        matchedPairs.Add(new MatchedCsvPair(
            leftEntry.Relative,
            rightEntry.Relative,
            leftEntry.Metadata.Length,
            rightEntry.Metadata.Length,
            CsvMatchKind.FileHash,
            contentIdentical: true,
            rowComparison: null,
            columnComparison: null));
        unmatchedLeftFiles.Remove(leftEntry.Relative);
        unmatchedRightFiles.Remove(rightEntry.Relative);
        unmatchedLeft.Remove(leftEntry.Relative);
        unmatchedRight.Remove(rightEntry.Relative);
    }

    private static Dictionary<string, List<HashEntry>> GroupByHash(
        Dictionary<string, CsvFileMetadata> relativeToFile)
    {
        var result = new Dictionary<string, List<HashEntry>>(StringComparer.Ordinal);

        foreach (var (relative, metadata) in relativeToFile)
        {
            var hash = metadata.ContentHash;
            if (!result.TryGetValue(hash, out var entries))
            {
                entries = [];
                result[hash] = entries;
            }

            entries.Add(new HashEntry(relative, metadata));
        }

        return result;
    }

    private void EnsureMetadataHaveDataRows(IReadOnlyDictionary<string, CsvFileMetadata> files)
    {
        foreach (var (relative, metadata) in files.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!metadata.HasDataRows)
            {
                throw new CsvDataException(
                    $"CSV file '{relative}' has no data rows.");
            }
        }
    }

    private void MatchByRelativePath(
        CsvTreeReadSession left,
        CsvTreeReadSession right,
        HashSet<string> unmatchedLeft,
        HashSet<string> unmatchedRight,
        List<MatchedCsvPair> matchedPairs)
    {
        if (unmatchedLeft.Count == 0 || unmatchedRight.Count == 0)
        {
            return;
        }

        var rightKeyByInsensitive = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rightRelative in unmatchedRight)
        {
            rightKeyByInsensitive[rightRelative] = rightRelative;
        }

        var sharedLeftRelatives = unmatchedLeft
            .Where(rightKeyByInsensitive.ContainsKey)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var leftRelative in sharedLeftRelatives)
        {
            var rightRelative = rightKeyByInsensitive[leftRelative];
            matchedPairs.Add(this.CompareRelativePathPair(left, right, leftRelative, rightRelative));
            unmatchedLeft.Remove(leftRelative);
            unmatchedRight.Remove(rightRelative);
        }
    }

    private MatchedCsvPair CompareRelativePathPair(
        CsvTreeReadSession left,
        CsvTreeReadSession right,
        string leftRelative,
        string rightRelative)
    {
        var leftLength = left.GetLength(leftRelative);
        var rightLength = right.GetLength(rightRelative);

        using var leftStream = left.OpenCsv(leftRelative);
        using var rightStream = right.OpenCsv(rightRelative);
        return CsvFile.MatchByStreaming(
            leftRelative,
            leftLength,
            leftStream,
            rightRelative,
            rightLength,
            rightStream,
            CsvMatchKind.RelativePath);
    }

    private void MatchByFileHash(
        Dictionary<string, CsvFileMetadata> unmatchedLeftFiles,
        Dictionary<string, CsvFileMetadata> unmatchedRightFiles,
        HashSet<string> unmatchedLeft,
        HashSet<string> unmatchedRight,
        List<MatchedCsvPair> matchedPairs)
    {
        // Hash pairing runs after relative-path matching. One-sided duplicate
        // hashes stay unmatched for the complex-key pass — but a *shared* hash
        // that is not uniquely 1:1 on both sides is an error.
        if (unmatchedLeftFiles.Count == 0 || unmatchedRightFiles.Count == 0)
        {
            return;
        }

        var leftByHash = GroupByHash(unmatchedLeftFiles);
        var rightByHash = GroupByHash(unmatchedRightFiles);

        foreach (var (hash, leftEntries) in leftByHash.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            if (!rightByHash.TryGetValue(hash, out var rightEntries))
            {
                continue;
            }

            if (leftEntries.Count == 1 && rightEntries.Count == 1)
            {
                MatchHashPair(
                    leftEntries[0],
                    rightEntries[0],
                    unmatchedLeftFiles,
                    unmatchedRightFiles,
                    unmatchedLeft,
                    unmatchedRight,
                    matchedPairs);
                continue;
            }

            // TODO: continue instead of throwing so MatchByFileNameAndColumns
            // can disambiguate, with leftovers reported as left_only / right_only.
            // Throwing here aborts the whole tree comparison for a common case.
            throw new CsvDataException(FormatDuplicateHashMessage(
                hash,
                leftEntries,
                rightEntries));
        }
    }

    private void MatchByFileNameAndColumns(
        CsvTreeReadSession left,
        CsvTreeReadSession right,
        Dictionary<string, CsvFileMetadata> unmatchedLeftFiles,
        Dictionary<string, CsvFileMetadata> unmatchedRightFiles,
        HashSet<string> unmatchedLeft,
        HashSet<string> unmatchedRight,
        List<MatchedCsvPair> matchedPairs)
    {
        // Signature pairing runs last. One-sided duplicate name+columns+rows
        // groups stay as left_only / right_only — but a *shared* signature that
        // is not uniquely 1:1 on both sides is an error.
        if (unmatchedLeftFiles.Count == 0 || unmatchedRightFiles.Count == 0)
        {
            return;
        }

        var leftBySignature = this.GroupBySignature(unmatchedLeftFiles);
        var rightBySignature = this.GroupBySignature(unmatchedRightFiles);

        foreach (var (signature, leftEntries) in leftBySignature.OrderBy(static pair => pair.Key.ToString(), StringComparer.Ordinal))
        {
            if (!rightBySignature.TryGetValue(signature, out var rightEntries))
            {
                continue;
            }

            if (leftEntries.Count == 1 && rightEntries.Count == 1)
            {
                this.MatchSignaturePair(
                    left,
                    right,
                    leftEntries[0],
                    rightEntries[0],
                    unmatchedLeftFiles,
                    unmatchedRightFiles,
                    unmatchedLeft,
                    unmatchedRight,
                    matchedPairs);
                continue;
            }

            // TODO: continue instead of throwing so leftovers are reported as
            // left_only / right_only instead of aborting the whole tree comparison.
            throw new CsvDataException(FormatDuplicateSignatureMessage(
                signature,
                leftEntries,
                rightEntries));
        }
    }

    private void MatchSignaturePair(
        CsvTreeReadSession left,
        CsvTreeReadSession right,
        SignatureEntry leftEntry,
        SignatureEntry rightEntry,
        Dictionary<string, CsvFileMetadata> unmatchedLeftFiles,
        Dictionary<string, CsvFileMetadata> unmatchedRightFiles,
        HashSet<string> unmatchedLeft,
        HashSet<string> unmatchedRight,
        List<MatchedCsvPair> matchedPairs)
    {
        using var leftStream = left.OpenCsv(leftEntry.Relative);
        using var rightStream = right.OpenCsv(rightEntry.Relative);
        matchedPairs.Add(CsvFile.MatchByStreaming(
            leftEntry.Relative,
            leftEntry.Metadata.Length,
            leftStream,
            rightEntry.Relative,
            rightEntry.Metadata.Length,
            rightStream,
            CsvMatchKind.FileNameAndColumns));
        unmatchedLeftFiles.Remove(leftEntry.Relative);
        unmatchedRightFiles.Remove(rightEntry.Relative);
        unmatchedLeft.Remove(leftEntry.Relative);
        unmatchedRight.Remove(rightEntry.Relative);
    }

    private Dictionary<CsvFileSignature, List<SignatureEntry>> GroupBySignature(
        Dictionary<string, CsvFileMetadata> relativeToFile)
    {
        var result = new Dictionary<CsvFileSignature, List<SignatureEntry>>();

        foreach (var (relative, metadata) in relativeToFile)
        {
            var fileName = this.fileSystem.Path.GetFileName(relative);
            var signature = metadata.CreateSignature(fileName);
            if (!result.TryGetValue(signature, out var entries))
            {
                entries = [];
                result[signature] = entries;
            }

            entries.Add(new SignatureEntry(relative, metadata));
        }

        return result;
    }

    private sealed record HashEntry(string Relative, CsvFileMetadata Metadata);

    private sealed record SignatureEntry(string Relative, CsvFileMetadata Metadata);
}
