// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;

public sealed class CsvComparisonOutput
{
    private readonly ILogger<CsvComparisonOutput> logger;
    private readonly IFileSystem fileSystem;
    private readonly CsvTreeComparer csvTreeComparer;
    private readonly HtmlLogDirectoryDiscovery htmlLogDirectoryDiscovery;

    public CsvComparisonOutput(
        ILogger<CsvComparisonOutput> logger,
        IFileSystem fileSystem,
        CsvTreeComparer csvTreeComparer,
        HtmlLogDirectoryDiscovery htmlLogDirectoryDiscovery)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(csvTreeComparer);
        Argument.ThrowIfNull(htmlLogDirectoryDiscovery);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.csvTreeComparer = csvTreeComparer;
        this.htmlLogDirectoryDiscovery = htmlLogDirectoryDiscovery;
    }

    public int CompareDirectories(string leftDir, string rightDir, bool verbose = false)
    {
        Argument.ThrowIfNull(leftDir);
        Argument.ThrowIfNull(rightDir);

        if (!this.IsCsvTreeSource(leftDir))
        {
            this.logger.LogError("error: not a directory or ZIP file: {LeftDir}", leftDir);
            return 2;
        }

        if (!this.IsCsvTreeSource(rightDir))
        {
            this.logger.LogError("error: not a directory or ZIP file: {RightDir}", rightDir);
            return 2;
        }

        CsvTreeComparison result;
        try
        {
            result = this.csvTreeComparer.CompareCsvTrees(leftDir, rightDir);
        }
        catch (Exception exc) when (exc is IOException
            or InvalidDataException
            or UnauthorizedAccessException
            or CsvDataException
            or CsvHelper.CsvHelperException)
        {
            this.logger.LogError(exc, "error: {Message}", exc.Message);
            return 2;
        }

        var status = ResolveComparisonStatus(result);
        var countsJson = FormatComparisonCountsIfNeeded(result, status, verbose);
        this.LogPairStatus(
            1,
            1,
            leftDir,
            result.LeftCsvCount,
            rightDir,
            result.RightCsvCount,
            status,
            countsJson);

        return status == CsvComparisonStatus.Equal ? 0 : 1;
    }

    public int CompareChain(string root, bool verbose = false)
    {
        Argument.ThrowIfNull(root);

        IReadOnlyList<string> dirs;
        try
        {
            dirs = this.htmlLogDirectoryDiscovery.DiscoverHtmlLogDirs(root);
        }
        catch (HtmlLogDiscoveryException exc)
        {
            this.logger.LogError("error: {Message}", exc.Message);
            return 2;
        }

        if (dirs.Count < 2)
        {
            this.logger.LogError("Need at least 2 HtmlLog folders or ZIP files; found {Count}.", dirs.Count);
            return 2;
        }

        this.logger.LogInformation("HtmlLog folders and ZIP files ({Count}):", dirs.Count);

        try
        {
            foreach (var dir in dirs)
            {
                var parent = this.fileSystem.Path.GetDirectoryName(dir)!;
                var paths = this.csvTreeComparer.CollectCsvPaths(dir);
                this.logger.LogInformation(
                    "  {FolderName}  ({CsvCount} csv)  [{Parent}]",
                    this.fileSystem.Path.GetFileName(dir),
                    paths.Count,
                    parent);
            }

            this.logger.LogInformation(string.Empty);

            var passed = 0;
            var partlyEqual = 0;
            var notEqual = 0;
            var total = dirs.Count - 1;

            for (var i = 0; i < total; i++)
            {
                var left = dirs[i];
                var right = dirs[i + 1];

                CsvComparisonStatus status;
                string? countsJson;
                int leftCount;
                int rightCount;
                {
                    var result = this.csvTreeComparer.CompareCsvTrees(left, right);
                    leftCount = result.LeftCsvCount;
                    rightCount = result.RightCsvCount;
                    status = ResolveComparisonStatus(result);
                    countsJson = FormatComparisonCountsIfNeeded(result, status, verbose);
                }

                switch (status)
                {
                    case CsvComparisonStatus.Equal:
                        passed++;
                        break;
                    case CsvComparisonStatus.PartlyEqual:
                        partlyEqual++;
                        break;
                    case CsvComparisonStatus.NotEqual:
                        notEqual++;
                        break;
                    default:
                        throw new UnreachableException($"Unexpected comparison status: {status}");
                }

                this.LogPairStatus(
                    i + 1,
                    total,
                    left,
                    leftCount,
                    right,
                    rightCount,
                    status,
                    countsJson);
            }

            this.logger.LogInformation(string.Empty);
            var failures = partlyEqual + notEqual;
            if (failures > 0)
            {
                this.logger.LogWarning(
                    "SUMMARY: {Total} chain comparisons, {Passed} equal, {PartlyEqual} partly equal, {NotEqual} not equal",
                    total,
                    passed,
                    partlyEqual,
                    notEqual);
            }
            else
            {
                this.logger.LogInformation(
                    "SUMMARY: {Total} chain comparisons, {Passed} equal, {PartlyEqual} partly equal, {NotEqual} not equal",
                    total,
                    passed,
                    partlyEqual,
                    notEqual);
            }

            return failures == 0 ? 0 : 1;
        }
        catch (Exception exc) when (exc is IOException
            or InvalidDataException
            or UnauthorizedAccessException
            or CsvDataException
            or CsvHelper.CsvHelperException)
        {
            this.logger.LogError(exc, "error: {Message}", exc.Message);
            return 2;
        }
    }

    public void WriteUsage()
    {
        this.logger.LogError(
            """
            Usage:
              HtmlLogCsvComparer [--verbose] --compare <left_source> <right_source>
              HtmlLogCsvComparer [--verbose] <left_source> <right_source>
              HtmlLogCsvComparer [--verbose] --chain <search_root>

            A comparison source is an HtmlLog_* folder or its .zip equivalent.
            Pair mode compares any folder/ZIP combination. Chain mode discovers
            both forms under a search root, orders them by the timestamp in their
            names, and compares each adjacent pair. Files are paired by relative
            path first, then by SHA-256 hash of parsed CSV content (BOM-stripped
            headers and parsed row fields) when that hash is unique on both
            sides, then by file name, CSV header columns, and data-row count for
            any leftovers. Duplicate shared hashes or signatures are an error.
            Every compared CSV must have at least one data row; a header-only
            file aborts the comparison (exit 2). Matched pairs are compared by
            parsed-field equality, not raw bytes — CSVs that differ only in
            quoting or line endings can still be content-identical. Unequal
            pairs are classified as partly equal when column sets differ but
            all rows match on the intersecting columns, otherwise as different.
            --verbose emits comparison JSON in both modes: compact match
            breakdown for EQUAL; expanded unmatched / partly-equal / different
            details otherwise. Both modes always emit compact counts JSON for
            unequal pairs even without --verbose. On the first load or compare
            error in chain mode, the tool logs the error and exits 2 (no
            SUMMARY); prior pair lines may already have been printed.
            """);
    }

    private static CsvComparisonStatus ResolveComparisonStatus(CsvTreeComparison result)
    {
        if (result.IsEqual)
        {
            return CsvComparisonStatus.Equal;
        }

        if (IsPartlyEqualOnly(result))
        {
            return CsvComparisonStatus.PartlyEqual;
        }

        return CsvComparisonStatus.NotEqual;
    }

    private static bool IsPartlyEqualOnly(CsvTreeComparison result) =>
        result.DifferentPairs.Count == 0
        && result.LeftOnlyRelativePaths.Count == 0
        && result.RightOnlyRelativePaths.Count == 0
        && result.PartlyEqualPairs.Count > 0;

    private static string? FormatComparisonCountsIfNeeded(
        CsvTreeComparison result,
        CsvComparisonStatus status,
        bool verbose) =>
        verbose || status != CsvComparisonStatus.Equal
            ? FormatComparisonCounts(result, verbose)
            : null;

    private static string FormatComparisonCounts(CsvTreeComparison result, bool verbose = false)
    {
        var pathMatched = result.MatchedPairs.Count(static pair => pair.MatchKind == CsvMatchKind.RelativePath);
        var hashMatched = result.MatchedPairs.Count(static pair => pair.MatchKind == CsvMatchKind.FileHash);
        var signatureMatched = result.MatchedPairs.Count(
            static pair => pair.MatchKind == CsvMatchKind.FileNameAndColumns);
        var matched = new
        {
            total = result.MatchedPairs.Count,
            by_path = pathMatched,
            by_hash = hashMatched,
            by_columns = signatureMatched,
        };

        if (result.IsEqual)
        {
            return JsonSerializer.Serialize(
                new { matched },
                new JsonSerializerOptions { WriteIndented = true });
        }

        if (!verbose)
        {
            return JsonSerializer.Serialize(
                new
                {
                    matching = new
                    {
                        matched,
                        unmatched_left = result.LeftOnlyRelativePaths.Count,
                        unmatched_right = result.RightOnlyRelativePaths.Count,
                    },
                    comparison = new
                    {
                        equal = result.ContentIdenticalPairs.Count,
                        partly_equal = result.PartlyEqualPairs.Count,
                        different = result.DifferentPairs.Count,
                    },
                },
                new JsonSerializerOptions { WriteIndented = true });
        }

        return JsonSerializer.Serialize(
            new
            {
                matching = new
                {
                    matched,
                    unmatched_left = new
                    {
                        total = result.LeftOnlyRelativePaths.Count,
                        files = result.LeftOnlyRelativePaths,
                    },
                    unmatched_right = new
                    {
                        total = result.RightOnlyRelativePaths.Count,
                        files = result.RightOnlyRelativePaths,
                    },
                },
                comparison = new
                {
                    equal = result.ContentIdenticalPairs.Count,
                    partly_equal = new
                    {
                        total = result.PartlyEqualPairs.Count,
                        groups = GroupPartlyEqualPairs(result.PartlyEqualPairs)
                            .Select(static group => new
                            {
                                left_only_columns = group.LeftOnlyColumns,
                                right_only_columns = group.RightOnlyColumns,
                                intersecting_columns = group.IntersectingColumns,
                                total = group.Pairs.Count,
                                pairs = group.Pairs
                                    .Select(static pair => new
                                    {
                                        left = pair.LeftRelativePath,
                                        right = pair.RightRelativePath,
                                    })
                                    .ToArray(),
                            })
                            .ToArray(),
                    },
                    different = new
                    {
                        total = result.DifferentPairs.Count,
                        files = result.DifferentPairs
                            .Select(static pair => new
                            {
                                left = pair.LeftRelativePath,
                                right = pair.RightRelativePath,
                            })
                            .ToArray(),
                    },
                },
            },
            new JsonSerializerOptions { WriteIndented = true });
    }

    private static IReadOnlyList<PartlyEqualColumnGroup> GroupPartlyEqualPairs(
        IReadOnlyList<MatchedCsvPair> partlyEqualPairs) =>
        partlyEqualPairs
            .GroupBy(
                static pair => new ColumnComparisonKey(
                    pair.ColumnComparison!.LeftOnlyColumns,
                    pair.ColumnComparison.RightOnlyColumns,
                    pair.ColumnComparison.IntersectingColumns),
                ColumnComparisonKeyComparer.Instance)
            .Select(static group => new PartlyEqualColumnGroup(
                group.Key.LeftOnlyColumns,
                group.Key.RightOnlyColumns,
                group.Key.IntersectingColumns,
                group.ToArray()))
            .ToArray();

    private void LogPairStatus(
        int index,
        int total,
        string leftPath,
        int leftCount,
        string rightPath,
        int rightCount,
        CsvComparisonStatus status,
        string? countsJson)
    {
        const string pairMessageBase =
            "[{Index}/{Total}] {LeftPath} ({LeftCount} csv) vs {RightPath} ({RightCount} csv) — {Status}";
        var statusDisplayName = status.ToDisplayName();
        if (countsJson is null)
        {
            this.logger.LogInformation(
                pairMessageBase,
                index,
                total,
                leftPath,
                leftCount,
                rightPath,
                rightCount,
                statusDisplayName);
            return;
        }

        if (status == CsvComparisonStatus.Equal)
        {
            this.logger.LogInformation(
                pairMessageBase + "\n{Counts}",
                index,
                total,
                leftPath,
                leftCount,
                rightPath,
                rightCount,
                statusDisplayName,
                countsJson);
            return;
        }

        this.logger.LogWarning(
            pairMessageBase + "\n{Counts}",
            index,
            total,
            leftPath,
            leftCount,
            rightPath,
            rightCount,
            statusDisplayName,
            countsJson);
    }

    private bool IsCsvTreeSource(string path) =>
        this.fileSystem.Directory.Exists(path)
        || (this.fileSystem.File.Exists(path) && CsvTreeSource.IsZipPath(this.fileSystem, path));

    private sealed record PartlyEqualColumnGroup(
        IReadOnlyList<string> LeftOnlyColumns,
        IReadOnlyList<string> RightOnlyColumns,
        IReadOnlyList<string> IntersectingColumns,
        IReadOnlyList<MatchedCsvPair> Pairs);

    private sealed record ColumnComparisonKey(
        IReadOnlyList<string> LeftOnlyColumns,
        IReadOnlyList<string> RightOnlyColumns,
        IReadOnlyList<string> IntersectingColumns);

    private sealed class ColumnComparisonKeyComparer : IEqualityComparer<ColumnComparisonKey>
    {
        public static readonly ColumnComparisonKeyComparer Instance = new();

        public bool Equals(ColumnComparisonKey? x, ColumnComparisonKey? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return ListsEqual(x.LeftOnlyColumns, y.LeftOnlyColumns)
                && ListsEqual(x.RightOnlyColumns, y.RightOnlyColumns)
                && ListsEqual(x.IntersectingColumns, y.IntersectingColumns);
        }

        public int GetHashCode(ColumnComparisonKey obj)
        {
            var hash = default(HashCode);
            AddListHash(ref hash, obj.LeftOnlyColumns);
            AddListHash(ref hash, obj.RightOnlyColumns);
            AddListHash(ref hash, obj.IntersectingColumns);
            return hash.ToHashCode();
        }

        private static bool ListsEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
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

        private static void AddListHash(ref HashCode hash, IReadOnlyList<string> values)
        {
            hash.Add(values.Count);
            foreach (var value in values)
            {
                hash.Add(value, StringComparer.Ordinal);
            }
        }
    }
}
