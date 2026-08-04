// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer.Tests;

using System.IO.Abstractions;
using System.IO.Compression;
using System.Text;
using FileSystem.TestSupport;
using Xunit;

public sealed class CsvTreeComparerTests
{
    [Fact]
    public void CollectCsvPaths_UsesInjectedFileSystem()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var nested = fileSystem.Path.Combine(root, "nested");
        fileSystem.Directory.CreateDirectory(nested);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(root, "a.csv"), "a");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(nested, "b.csv"), "b");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(root, "ignore.txt"), "x");

        var comparer = new CsvTreeComparer(fileSystem);
        var paths = comparer.CollectCsvPaths(root);

        Assert.Equal(2, paths.Count);
        Assert.True(paths.ContainsKey("a.csv"));
        Assert.True(paths.ContainsKey("nested/b.csv"));
    }

    [Fact]
    public void CompareCsvTrees_MatchesByRelativePathWhenSamePathAndContent()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "A,B\nrow1,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "A,B\nrow1,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "only-left.csv"),
            "A,B\nleft,1\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        Assert.Single(result.MatchedPairs);
        Assert.Equal(CsvMatchKind.RelativePath, result.MatchedPairs[0].MatchKind);
        Assert.True(result.MatchedPairs[0].ContentIdentical);
        Assert.Single(result.LeftOnlyRelativePaths);
        Assert.Empty(result.RightOnlyRelativePaths);
        Assert.Empty(result.ContentDifferentPairs);
    }

    [Fact]
    public void CompareCsvTrees_MatchesByFileHashWhenPathsAndNamesDiffer()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        var leftNested = fileSystem.Path.Combine(left, "old-layout");
        var rightNested = fileSystem.Path.Combine(right, "new-layout");
        fileSystem.Directory.CreateDirectory(leftNested);
        fileSystem.Directory.CreateDirectory(rightNested);
        const string content = "DateTime,Temperature\n2020-01-01,1\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(leftNested, "Kyiv.csv"), content);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(rightNested, "Capital.csv"), content);

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.Equal(CsvMatchKind.FileHash, pair.MatchKind);
        Assert.True(pair.ContentIdentical);
        Assert.Equal("old-layout/Kyiv.csv", pair.LeftRelativePath);
        Assert.Equal("new-layout/Capital.csv", pair.RightRelativePath);
        Assert.True(result.IsEqual);
    }

    [Fact]
    public void CompareCsvTrees_MatchesByRelativePathWhenContentDiffers()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "A,B\nrow1,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "A,B\nrow1,1\nrow2,2\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.Equal(CsvMatchKind.RelativePath, pair.MatchKind);
        Assert.False(pair.ContentIdentical);
        Assert.Empty(result.LeftOnlyRelativePaths);
        Assert.Empty(result.RightOnlyRelativePaths);
        Assert.NotNull(pair.RowComparison);
        Assert.Equal(1, pair.RowComparison.ExistingRowCount);
        Assert.Equal(2, pair.RowComparison.DestinationRowCount);
    }

    [Fact]
    public void CompareCsvTrees_MatchesByRelativePath_WhenLeftHasMoreRowsThanRight()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "A,B\nrow1,1\nrow2,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "A,B\nrow1,1\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.Equal(CsvMatchKind.RelativePath, pair.MatchKind);
        Assert.False(pair.ContentIdentical);
        Assert.False(pair.IsPartlyEqual);
        Assert.Empty(result.PartlyEqualPairs);
        Assert.Empty(result.LeftOnlyRelativePaths);
        Assert.Empty(result.RightOnlyRelativePaths);
        Assert.NotNull(pair.RowComparison);
        Assert.Equal(2, pair.RowComparison.ExistingRowCount);
        Assert.Equal(1, pair.RowComparison.DestinationRowCount);
        Assert.Equal(1, pair.RowComparison.EqualRowCount);
    }

    [Fact]
    public void CompareCsvTrees_MatchesByFileHashWhenPathsDifferAndContentIdentical()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        var leftNested = fileSystem.Path.Combine(left, "old-layout");
        var rightNested = fileSystem.Path.Combine(right, "new-layout");
        fileSystem.Directory.CreateDirectory(leftNested);
        fileSystem.Directory.CreateDirectory(rightNested);
        const string headerAndRows = "DateTime,Temperature\n2020-01-01,1\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(leftNested, "Kyiv.csv"), headerAndRows);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(rightNested, "Kyiv.csv"), headerAndRows);

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        Assert.Single(result.MatchedPairs);
        Assert.Equal(CsvMatchKind.FileHash, result.MatchedPairs[0].MatchKind);
        Assert.True(result.MatchedPairs[0].ContentIdentical);
        Assert.Empty(result.LeftOnlyRelativePaths);
        Assert.Empty(result.RightOnlyRelativePaths);
        Assert.True(result.IsEqual);
    }

    [Fact]
    public void CompareCsvTrees_DoesNotMatchByFileHash_WhenOldDelimiterEncodingWouldCollide()
    {
        var unitSeparator = "\u001F";
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "b"));
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "a", "ambiguous.csv"),
            "a,b\n1,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "b", "ambiguous.csv"),
            $"\"a{unitSeparator}b\"\n\"1{unitSeparator}2\"\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        Assert.DoesNotContain(result.MatchedPairs, pair => pair.MatchKind == CsvMatchKind.FileHash);
        Assert.Equal("a/ambiguous.csv", Assert.Single(result.LeftOnlyRelativePaths));
        Assert.Equal("b/ambiguous.csv", Assert.Single(result.RightOnlyRelativePaths));
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenSharedHashIsDuplicated()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "b"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "c"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "d"));
        const string content = "DateTime,Temperature\n2020-01-01,1\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "a", "Kyiv.csv"), content);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "b", "Odesa.csv"), content);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "c", "Lviv.csv"), content);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "d", "Kharkiv.csv"), content);

        var comparer = new CsvTreeComparer(fileSystem);

        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("duplicate file-hash signature", exc.Message, StringComparison.Ordinal);
        Assert.Contains("left=[", exc.Message, StringComparison.Ordinal);
        Assert.Contains("right=[", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenSharedHashHasMultipleFilesOnOneSideOnly()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "b"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "c"));
        const string content = "DateTime,Temperature\n2020-01-01,1\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "a", "Kyiv.csv"), content);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "b", "Odesa.csv"), content);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "c", "Lviv.csv"), content);

        var comparer = new CsvTreeComparer(fileSystem);

        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("duplicate file-hash signature", exc.Message, StringComparison.Ordinal);
        Assert.Contains("left=[", exc.Message, StringComparison.Ordinal);
        Assert.Contains("right=[", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenSharedSignatureIsDuplicated()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "b"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "c"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "d"));

        // Same name+columns+rows, different bytes so hash matching does not claim them.
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "a", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "b", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,2\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "c", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,3\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "d", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,4\n");

        var comparer = new CsvTreeComparer(fileSystem);

        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("Kyiv.csv", exc.Message, StringComparison.Ordinal);
        Assert.Contains("left=[", exc.Message, StringComparison.Ordinal);
        Assert.Contains("right=[", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenSharedSignatureHasMultipleFilesOnOneSideOnly()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "b"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "c"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "a", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "b", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,2\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "c", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,3\n");

        var comparer = new CsvTreeComparer(fileSystem);

        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("Kyiv.csv", exc.Message, StringComparison.Ordinal);
        Assert.Contains("left=[", exc.Message, StringComparison.Ordinal);
        Assert.Contains("right=[", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_DoesNotThrowWhenDuplicateSignatureExistsOnlyOnOneSide()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "b"));
        fileSystem.Directory.CreateDirectory(right);
        const string kyivContent = "DateTime,Temperature\n2020-01-01,1\n";
        const string odesaContent = "DateTime,Humidity\n2020-01-01,50\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "a", "Kyiv.csv"), kyivContent);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "b", "Kyiv.csv"), kyivContent);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "Odesa.csv"), odesaContent);

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        Assert.Empty(result.MatchedPairs);
        Assert.Equal(2, result.LeftOnlyRelativePaths.Count);
        Assert.Contains("a/Kyiv.csv", result.LeftOnlyRelativePaths);
        Assert.Contains("b/Kyiv.csv", result.LeftOnlyRelativePaths);
        Assert.Equal(new[] { "Odesa.csv" }, result.RightOnlyRelativePaths);
    }

    [Fact]
    public void CollectCsvPaths_IncludesUppercaseCsvExtensionInDirectories()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        fileSystem.Directory.CreateDirectory(root);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(root, "Kyiv.CSV"), "DateTime,Temperature\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(root, "ignore.txt"), "x");

        var comparer = new CsvTreeComparer(fileSystem);
        var paths = comparer.CollectCsvPaths(root);

        Assert.True(paths.ContainsKey("Kyiv.CSV"));
        Assert.Single(paths);
    }

    [Fact]
    public void CompareCsvTrees_DoesNotMatchByFileNameAndColumnsWhenColumnHeadersDifferOnlyByCase()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        var leftNested = fileSystem.Path.Combine(left, "old-layout");
        var rightNested = fileSystem.Path.Combine(right, "new-layout");
        fileSystem.Directory.CreateDirectory(leftNested);
        fileSystem.Directory.CreateDirectory(rightNested);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(leftNested, "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(rightNested, "Kyiv.csv"),
            "datetime,temperature\n2020-01-01,1\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        Assert.Empty(result.MatchedPairs);
        Assert.Equal(new[] { "old-layout/Kyiv.csv" }, result.LeftOnlyRelativePaths);
        Assert.Equal(new[] { "new-layout/Kyiv.csv" }, result.RightOnlyRelativePaths);
        Assert.False(result.IsEqual);
    }

    [Fact]
    public void CompareCsvTrees_ReportsExistingDestinationAndEqualRowsWhenContentDiffers()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "A,B\nrow1,1\nrow2,2\nrow3,3\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "A,B\nrow1,1\nrow2,9\nrow3,3\nrow4,4\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        Assert.False(result.IsEqual);
        Assert.Single(result.ContentDifferentPairs);
        var stats = result.ContentDifferentPairs[0].RowComparison;
        Assert.NotNull(stats);
        Assert.Equal(3, stats.ExistingRowCount);
        Assert.Equal(4, stats.DestinationRowCount);
        Assert.Equal(2, stats.EqualRowCount);
    }

    [Fact]
    public void CompareCsvTrees_DoesNotMatchByFileNameAndColumnsWhenRowCountsDiffer()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        var leftNested = fileSystem.Path.Combine(left, "old-layout");
        var rightNested = fileSystem.Path.Combine(right, "new-layout");
        fileSystem.Directory.CreateDirectory(leftNested);
        fileSystem.Directory.CreateDirectory(rightNested);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(leftNested, "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(rightNested, "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,1\n2020-01-02,2\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        Assert.Empty(result.MatchedPairs);
        Assert.Equal(new[] { "old-layout/Kyiv.csv" }, result.LeftOnlyRelativePaths);
        Assert.Equal(new[] { "new-layout/Kyiv.csv" }, result.RightOnlyRelativePaths);
    }

    [Fact]
    public void CompareCsvTrees_TreatsLargeIdenticalDirectoryAndZipAsEqual()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var directory = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-01_00-00-00");
        var nested = fileSystem.Path.Combine(directory, "parsed");
        var zipPath = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-02_00-00-00.zip");
        fileSystem.Directory.CreateDirectory(nested);
        var content = "DateTime,Temperature\n" + string.Concat(
            Enumerable.Range(0, 5000).Select(i => $"2020-01-01,{i}\n"));
        Assert.True(Encoding.UTF8.GetByteCount(content) > 65536);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(nested, "Kyiv.csv"), content);
        CreateZip(
            fileSystem,
            zipPath,
            ("HtmlLog_2026-01-02_00-00-00/parsed/Kyiv.csv", content));

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(directory, zipPath);

        Assert.True(result.IsEqual);
        Assert.Single(result.MatchedPairs);
        Assert.True(result.MatchedPairs[0].ContentIdentical);
    }

    [Fact]
    public void CompareCsvTrees_TreatsWrappedZipAsEquivalentDirectory()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var directory = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-01_00-00-00");
        var nested = fileSystem.Path.Combine(directory, "parsed");
        var zipPath = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-02_00-00-00.zip");
        fileSystem.Directory.CreateDirectory(nested);
        const string content = "DateTime,Temperature\n2020-01-01,1\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(nested, "Kyiv.csv"), content);
        CreateZip(
            fileSystem,
            zipPath,
            ("HtmlLog_2026-01-02_00-00-00/parsed/Kyiv.csv", content),
            ("HtmlLog_2026-01-02_00-00-00/ignore.txt", "ignored"));

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(directory, zipPath);

        Assert.True(result.IsEqual);
        Assert.Single(result.MatchedPairs);
        Assert.Equal("parsed/Kyiv.csv", result.MatchedPairs[0].RightRelativePath);
    }

    [Fact]
    public void CompareCsvTrees_StripsWrapperFolderCaseInsensitively()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var directory = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-01_00-00-00");
        var nested = fileSystem.Path.Combine(directory, "parsed");
        var zipPath = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-02_00-00-00.zip");
        fileSystem.Directory.CreateDirectory(nested);
        const string content = "DateTime,Temperature\n2020-01-01,1\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(nested, "Kyiv.csv"), content);
        CreateZip(
            fileSystem,
            zipPath,
            ("htmllog_2026-01-02_00-00-00/parsed/Kyiv.csv", content));

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(directory, zipPath);

        Assert.True(result.IsEqual);
        Assert.Single(result.MatchedPairs);
        Assert.Equal("parsed/Kyiv.csv", result.MatchedPairs[0].RightRelativePath);
        Assert.Equal(CsvMatchKind.RelativePath, result.MatchedPairs[0].MatchKind);
    }

    [Fact]
    public void CompareCsvTrees_ComparesRootAndWrappedZipContents()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var leftZip = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-01_00-00-00.zip");
        var rightZip = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-02_00-00-00.zip");
        CreateZip(
            fileSystem,
            leftZip,
            ("Kyiv.csv", "A,B\nrow1,1\nrow2,2\n"));
        CreateZip(
            fileSystem,
            rightZip,
            ("HtmlLog_2026-01-02_00-00-00/Kyiv.csv", "A,B\nrow1,1\nrow2,9\nrow3,3\n"));

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(leftZip, rightZip);

        var pair = Assert.Single(result.ContentDifferentPairs);
        Assert.Equal(2, pair.RowComparison!.ExistingRowCount);
        Assert.Equal(3, pair.RowComparison.DestinationRowCount);
        Assert.Equal(1, pair.RowComparison.EqualRowCount);
    }

    [Fact]
    public void CompareCsvTrees_MatchesByRelativePathCaseInsensitively()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        var leftNested = fileSystem.Path.Combine(left, "Parsed");
        var rightNested = fileSystem.Path.Combine(right, "parsed");
        fileSystem.Directory.CreateDirectory(leftNested);
        fileSystem.Directory.CreateDirectory(rightNested);
        const string content = "DateTime,Temperature\n2020-01-01,1\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(leftNested, "Kyiv.csv"), content);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(rightNested, "kyiv.csv"), content);

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.Equal(CsvMatchKind.RelativePath, pair.MatchKind);
        Assert.True(pair.ContentIdentical);
        Assert.Equal("Parsed/Kyiv.csv", pair.LeftRelativePath);
        Assert.Equal("parsed/kyiv.csv", pair.RightRelativePath);
        Assert.True(result.IsEqual);
    }

    [Fact]
    public void CompareCsvTrees_PathMatchedEqual_DoesNotRequireHashFallback()
    {
        // Path-identical trees should compare via streaming equality without loading
        // leftover files for hash/signature matching.
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "a.csv"),
            "A,B\n1,2\n3,4\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "a.csv"),
            "A,B\n1,2\n3,4\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "b.csv"),
            "X\ny\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "b.csv"),
            "X\ny\n");

        var result = new CsvTreeComparer(fileSystem).CompareCsvTrees(left, right);

        Assert.Equal(2, result.MatchedPairs.Count);
        Assert.All(result.MatchedPairs, pair => Assert.Equal(CsvMatchKind.RelativePath, pair.MatchKind));
        Assert.All(result.MatchedPairs, pair => Assert.True(pair.ContentIdentical));
        Assert.True(result.IsEqual);
    }

    [Fact]
    public void CompareCsvTrees_PathMatchedUnequal_ComputesDetailStats()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "A,B\nrow1,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "A,B,C\nrow1,1,extra\n");

        var result = new CsvTreeComparer(fileSystem).CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.Equal(CsvMatchKind.RelativePath, pair.MatchKind);
        Assert.False(pair.ContentIdentical);
        Assert.True(pair.IsPartlyEqual);
        Assert.NotNull(pair.ColumnComparison);
        Assert.Equal(new[] { "C" }, pair.ColumnComparison.RightOnlyColumns);
        Assert.Single(result.PartlyEqualPairs);
    }

    [Fact]
    public void CompareCsvTrees_SignatureMatchedUnequal_ComputesDetailStatsWithoutContentIdentity()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        var leftNested = fileSystem.Path.Combine(left, "old-layout");
        var rightNested = fileSystem.Path.Combine(right, "new-layout");
        fileSystem.Directory.CreateDirectory(leftNested);
        fileSystem.Directory.CreateDirectory(rightNested);

        // Same file name + columns + row count, different values → hash miss, signature match.
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(leftNested, "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,1\n2020-01-02,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(rightNested, "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,1\n2020-01-02,99\n");

        var result = new CsvTreeComparer(fileSystem).CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.Equal(CsvMatchKind.FileNameAndColumns, pair.MatchKind);
        Assert.False(pair.ContentIdentical);
        Assert.False(pair.IsPartlyEqual);
        Assert.NotNull(pair.RowComparison);
        Assert.Equal(2, pair.RowComparison.ExistingRowCount);
        Assert.Equal(2, pair.RowComparison.DestinationRowCount);
        Assert.Equal(1, pair.RowComparison.EqualRowCount);
        Assert.NotNull(pair.ColumnComparison);
        Assert.Empty(pair.ColumnComparison.LeftOnlyColumns);
        Assert.Empty(pair.ColumnComparison.RightOnlyColumns);
        Assert.False(pair.ColumnComparison.IntersectionRowsEqual);
        Assert.Empty(result.LeftOnlyRelativePaths);
        Assert.Empty(result.RightOnlyRelativePaths);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenDirectoryHasCaseOnlyDuplicatePaths()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "Kyiv.csv"), "A,B\n1,2\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "kyiv.csv"), "A,B\n3,4\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "Kyiv.csv"), "A,B\n1,2\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var exc = Assert.Throws<InvalidDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("duplicate CSV path", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_StripsLeadingDotSlashFromZipEntries()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var directory = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-01_00-00-00");
        var nested = fileSystem.Path.Combine(directory, "parsed");
        var zipPath = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-02_00-00-00.zip");
        fileSystem.Directory.CreateDirectory(nested);
        const string content = "DateTime,Temperature\n2020-01-01,1\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(nested, "Kyiv.csv"), content);
        CreateZip(
            fileSystem,
            zipPath,
            ("./HtmlLog_2026-01-02_00-00-00/parsed/Kyiv.csv", content));

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(directory, zipPath);

        Assert.True(result.IsEqual);
        Assert.Single(result.MatchedPairs);
        Assert.Equal("parsed/Kyiv.csv", result.MatchedPairs[0].RightRelativePath);
        Assert.Equal(CsvMatchKind.RelativePath, result.MatchedPairs[0].MatchKind);
    }

    [Theory]
    [InlineData("/HtmlLog_2026-01-02_00-00-00/parsed/Kyiv.csv")]
    [InlineData("C:/HtmlLog_2026-01-02_00-00-00/parsed/Kyiv.csv")]
    [InlineData(@"\\server\share\HtmlLog_2026-01-02_00-00-00\parsed\Kyiv.csv")]
    [InlineData("//server/share/HtmlLog_2026-01-02_00-00-00/parsed/Kyiv.csv")]
    public void CompareCsvTrees_ThrowsWhenZipEntryPathIsRootedOrUnc(string entryPath)
    {
        var fileSystem = InMemoryFileSystem.Create();
        var directory = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-01_00-00-00");
        var nested = fileSystem.Path.Combine(directory, "parsed");
        var zipPath = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-02_00-00-00.zip");
        fileSystem.Directory.CreateDirectory(nested);
        const string content = "DateTime,Temperature\n2020-01-01,1\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(nested, "Kyiv.csv"), content);
        CreateZip(fileSystem, zipPath, (entryPath, content));

        var comparer = new CsvTreeComparer(fileSystem);
        var exc = Assert.Throws<InvalidDataException>(() => comparer.CompareCsvTrees(directory, zipPath));
        Assert.Contains("archive-relative", exc.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenZipHasDuplicateRelativePathsAfterWrapperStrip()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var zipPath = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-02_00-00-00.zip");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "Kyiv.csv"), "A,B\n1,2\n");
        CreateZip(
            fileSystem,
            zipPath,
            ("Kyiv.csv", "A,B\n1,2\n"),
            ("HtmlLog_2026-01-02_00-00-00/Kyiv.csv", "A,B\n3,4\n"));

        var comparer = new CsvTreeComparer(fileSystem);
        var exc = Assert.Throws<InvalidDataException>(() => comparer.CompareCsvTrees(left, zipPath));
        Assert.Contains("duplicate CSV path", exc.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../../other/Kyiv.csv")]
    [InlineData("HtmlLog_2026-01-02_00-00-00/../outside/Kyiv.csv")]
    [InlineData("parsed/../../Kyiv.csv")]
    public void CompareCsvTrees_ThrowsWhenZipEntryPathContainsParentDirectorySegment(string entryPath)
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var zipPath = InMemoryFileSystem.UnderRoot(fileSystem, "HtmlLog_2026-01-02_00-00-00.zip");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "Kyiv.csv"), "A,B\n1,2\n");
        CreateZip(fileSystem, zipPath, (entryPath, "A,B\n1,2\n"));

        var comparer = new CsvTreeComparer(fileSystem);
        var exc = Assert.Throws<InvalidDataException>(() => comparer.CompareCsvTrees(left, zipPath));
        Assert.Contains("parent-directory segment '..'", exc.Message, StringComparison.Ordinal);
        Assert.Contains(entryPath, exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenLeftFileHasDuplicateColumns()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "DateTime,Temperature,DateTime\n2020-01-01,1,2020-01-01\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");

        var comparer = new CsvTreeComparer(fileSystem);

        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("duplicate column 'DateTime'", exc.Message, StringComparison.Ordinal);
        Assert.Contains("shared.csv", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenRightFileHasDuplicateColumns()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "DateTime,Temperature,Temperature\n2020-01-01,1,1\n");

        var comparer = new CsvTreeComparer(fileSystem);

        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("duplicate column 'Temperature'", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_PreservesLeadingFeFfInNonFirstHeaderFields()
    {
        var feffA = "\uFEFFA";
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        var csv = $"A,\"{feffA}\"\n1,2\n";
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "shared.csv"), csv);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "shared.csv"), csv);

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.True(pair.ContentIdentical);
        Assert.True(result.IsEqual);
    }

    [Fact]
    public void CompareCsvTrees_LoadsUtf8BomPrefixedFiles()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        var csv = "DateTime,Temperature\n2020-01-01,1\n";
        var bomPrefixedCsv = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        fileSystem.File.WriteAllBytes(fileSystem.Path.Combine(left, "shared.csv"), bomPrefixedCsv);
        fileSystem.File.WriteAllBytes(fileSystem.Path.Combine(right, "shared.csv"), bomPrefixedCsv);

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.True(pair.ContentIdentical);
        Assert.True(result.IsEqual);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenLeftFileHasNoDataRows()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "DateTime,Temperature\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");

        var comparer = new CsvTreeComparer(fileSystem);

        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("shared.csv", exc.Message, StringComparison.Ordinal);
        Assert.Contains("no data rows", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenRightFileHasNoDataRows()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "DateTime,Temperature\n");

        var comparer = new CsvTreeComparer(fileSystem);

        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("shared.csv", exc.Message, StringComparison.Ordinal);
        Assert.Contains("no data rows", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_ClassifiesAsPartlyEqualWhenExtraColumnAndIntersectingRowsMatch()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "Place,DateTime,Temperature\nKyiv,2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.True(pair.IsPartlyEqual);
        Assert.Single(result.PartlyEqualPairs);
        Assert.Empty(result.DifferentPairs);
        Assert.False(result.IsEqual);
        Assert.NotNull(pair.ColumnComparison);
        Assert.Equal(new[] { "Place" }, pair.ColumnComparison.LeftOnlyColumns);
        Assert.Empty(pair.ColumnComparison.RightOnlyColumns);
        Assert.Equal(new[] { "DateTime", "Temperature" }, pair.ColumnComparison.IntersectingColumns);
        Assert.True(pair.ColumnComparison.IntersectionRowsEqual);
    }

    [Fact]
    public void CompareCsvTrees_ClassifiesAsPartlyEqualWhenColumnNameDiffersOnlyByCase()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "DateTime,Temp\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "DateTime,temp\n2020-01-01,1\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.True(pair.IsPartlyEqual);
        Assert.Single(result.PartlyEqualPairs);
        Assert.Empty(result.DifferentPairs);
        Assert.False(result.IsEqual);
        Assert.NotNull(pair.ColumnComparison);
        Assert.Equal(new[] { "Temp" }, pair.ColumnComparison.LeftOnlyColumns);
        Assert.Equal(new[] { "temp" }, pair.ColumnComparison.RightOnlyColumns);
        Assert.Equal(new[] { "DateTime" }, pair.ColumnComparison.IntersectingColumns);
        Assert.True(pair.ColumnComparison.IntersectionRowsEqual);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenDataRowHasFewerFieldsThanHeader()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "A,B,C\n1,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "A,B,C\n1,2,x\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("shared.csv", exc.Message, StringComparison.Ordinal);
        Assert.Contains("2 fields", exc.Message, StringComparison.Ordinal);
        Assert.Contains("3 header columns", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsWhenDataRowHasMoreFieldsThanHeader()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "A\n1,secret\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "A,B\n1,public\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("shared.csv", exc.Message, StringComparison.Ordinal);
        Assert.Contains("2 fields", exc.Message, StringComparison.Ordinal);
        Assert.Contains("1 header columns", exc.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareCsvTrees_ClassifiesAsDifferentWhenExtraColumnAndIntersectingCellDiffers()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "Place,DateTime,Temperature\nKyiv,2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "DateTime,Temperature\n2020-01-01,2\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.False(pair.IsPartlyEqual);
        Assert.Empty(result.PartlyEqualPairs);
        Assert.Single(result.DifferentPairs);
        Assert.NotNull(pair.ColumnComparison);
        Assert.Equal(new[] { "Place" }, pair.ColumnComparison.LeftOnlyColumns);
        Assert.False(pair.ColumnComparison.IntersectionRowsEqual);
    }

    [Fact]
    public void CompareCsvTrees_ClassifiesAsDifferentWhenExtraColumnAndRowCountsDiffer()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "Place,DateTime,Temperature\nKyiv,2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "DateTime,Temperature\n2020-01-01,1\n2020-01-02,2\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.False(pair.IsPartlyEqual);
        Assert.Empty(result.PartlyEqualPairs);
        Assert.Single(result.DifferentPairs);
        Assert.NotNull(pair.RowComparison);
        Assert.Equal(1, pair.RowComparison.ExistingRowCount);
        Assert.Equal(2, pair.RowComparison.DestinationRowCount);
    }

    [Fact]
    public void CompareCsvTrees_TreatsCrLfAndLfOnlyFormattingDifferenceAsContentIdentical()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "shared.csv"),
            "DateTime,Temperature\r\n2020-01-01,1\r\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "shared.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");

        var comparer = new CsvTreeComparer(fileSystem);
        var result = comparer.CompareCsvTrees(left, right);

        var pair = Assert.Single(result.MatchedPairs);
        Assert.True(pair.ContentIdentical);
        Assert.Empty(result.ContentDifferentPairs);
        Assert.True(result.IsEqual);
    }

    [Fact]
    public void CompareCsvTrees_ThrowsDuplicateColumnWhenLoadingUnmatchedLeftOnlyFile()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "only-left.csv"),
            "DateTime,Temperature,DateTime\n2020-01-01,1,2020-01-01\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "only-right.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");

        var comparer = new CsvTreeComparer(fileSystem);

        var exc = Assert.Throws<CsvDataException>(() => comparer.CompareCsvTrees(left, right));
        Assert.Contains("duplicate column 'DateTime'", exc.Message, StringComparison.Ordinal);
        Assert.Contains("only-left.csv", exc.Message, StringComparison.Ordinal);
    }

    private static void CreateZip(
        IFileSystem fileSystem,
        string path,
        params (string Path, string Content)[] entries)
    {
        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
        using var stream = fileSystem.File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var (entryPath, content) in entries)
        {
            var entry = archive.CreateEntry(entryPath);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
    }
}
