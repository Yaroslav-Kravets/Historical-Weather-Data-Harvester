// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.SourceFileSystem.Tests;

using System.IO.Abstractions;
using System.Text;
using FileSystem.TestSupport;
using Xunit;

public sealed class SevenZipSourceFileSystemTests
{
    private readonly IFileSystem host = InMemoryFileSystem.Create();

    public SevenZipSourceFileSystemTests()
    {
        this.host.Directory.CreateDirectory(InMemoryFileSystem.Root);
    }

    [Fact]
    public void GetFiles_And_FileExists_UseArchiveIndex()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            ("Kyiv/2003-01-01.html", "one"),
            ("Kyiv/2003-01-02.html", "two"));

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);

        Assert.False(source.SupportsParallel);
        Assert.Equal(new[] { "Kyiv/2003-01-01.html", "Kyiv/2003-01-02.html" }, source.GetFiles());
        Assert.True(source.FileExists("Kyiv/2003-01-01.html"));
        Assert.False(source.FileExists("missing.html"));
    }

    [Fact]
    public void GetFiles_ReturnsImmutableCopy()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            ("Kyiv/2003-01-01.html", "one"),
            ("Kyiv/2003-01-02.html", "two"));

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);
        var files = source.GetFiles();
        Assert.IsAssignableFrom<string[]>(files);

        var mutable = (string[])files;
        mutable[0] = "mutated.html";

        Assert.Equal(new[] { "Kyiv/2003-01-01.html", "Kyiv/2003-01-02.html" }, source.GetFiles());
        Assert.True(source.FileExists("Kyiv/2003-01-01.html"));
        Assert.False(source.FileExists("mutated.html"));
    }

    [Fact]
    public void OpenAll_ReadsEntriesSequentially()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            ("Kyiv/2003-01-01.html", "one"),
            ("Kyiv/2003-01-02.html", "two"));

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);
        using var enumerator = source.OpenAll().GetEnumerator();

        Assert.True(enumerator.MoveNext());
        using (var file = enumerator.Current)
        {
            Assert.Equal("Kyiv/2003-01-01.html", file.Path);
            using var reader = new StreamReader(file.Content, Encoding.UTF8);
            Assert.Equal("one", reader.ReadToEnd());
        }

        Assert.True(enumerator.MoveNext());
        using (var file = enumerator.Current)
        {
            Assert.Equal("Kyiv/2003-01-02.html", file.Path);
            using var reader = new StreamReader(file.Content, Encoding.UTF8);
            Assert.Equal("two", reader.ReadToEnd());
        }

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void OpenAll_Throws_WhenPreviousEntryStreamIsStillOpen()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            ("Kyiv/2003-01-01.html", "one"),
            ("Kyiv/2003-01-02.html", "two"));

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);
        using var enumerator = source.OpenAll().GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;

        var exception = Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Contains("must be disposed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenAll_CanBeReopened_AfterPreviousEnumerationCompletes()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            ("Kyiv/2003-01-01.html", "one"),
            ("Kyiv/2003-01-02.html", "two"));

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);
        var expected = new[]
        {
            ("Kyiv/2003-01-01.html", "one"),
            ("Kyiv/2003-01-02.html", "two"),
        };

        for (var pass = 0; pass < 3; pass++)
        {
            var opened = new List<(string Path, string Content)>();
            foreach (var file in source.OpenAll())
            {
                using (file)
                using (var reader = new StreamReader(file.Content, Encoding.UTF8))
                {
                    opened.Add((file.Path, reader.ReadToEnd()));
                }
            }

            Assert.Equal(expected, opened);
        }
    }

    [Fact]
    public void OpenAll_Throws_WhenAnotherEnumerationIsInProgress()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            ("Kyiv/2003-01-01.html", "one"),
            ("Kyiv/2003-01-02.html", "two"));

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);
        using var first = source.OpenAll().GetEnumerator();
        Assert.True(first.MoveNext());
        first.Current.Dispose();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var second = source.OpenAll().GetEnumerator();
            second.MoveNext();
        });
        Assert.Contains("already in progress", exception.Message, StringComparison.OrdinalIgnoreCase);

        first.Dispose();

        var opened = new List<string>();
        foreach (var file in source.OpenAll())
        {
            using (file)
            {
                opened.Add(file.Path);
            }
        }

        Assert.Equal(new[] { "Kyiv/2003-01-01.html", "Kyiv/2003-01-02.html" }, opened);
    }

    [Fact]
    public void Factory_OpensSevenZipSource_ForArchivePath()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            ("Kyiv/2003-01-01.html", "one"));

        using var source = SourceFileSystemFactory.Open(this.host, archivePath);
        Assert.IsType<SevenZipSourceFileSystem>(source);
        Assert.False(source.SupportsParallel);
    }

    [Fact]
    public void GetFiles_Throws_WhenArchiveMissing()
    {
        var missing = InMemoryFileSystem.UnderRoot(this.host, "missing.7z");
        using var source = new SevenZipSourceFileSystem(this.host, missing);
        Assert.Throws<FileNotFoundException>(() => source.GetFiles());
    }

    [Theory]
    [InlineData("/Kyiv/file.html")]
    [InlineData(@"C:\Kyiv\file.html")]
    [InlineData(@"\\server\share\file.html")]
    [InlineData("//server/share/file.html")]
    public void FileExists_Throws_WhenPathIsRootedOrUnc(string rootedPath)
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            ("Kyiv/2003-01-01.html", "one"));

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);
        var exception = Assert.Throws<InvalidDataException>(() => source.FileExists(rootedPath));
        Assert.Contains("archive-relative", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/Kyiv/x.html")]
    [InlineData(@"C:\Kyiv\x.html")]
    [InlineData(@"\\server\share\x.html")]
    [InlineData("//server/share/x.html")]
    public void GetFiles_Throws_WhenArchiveEntryKeyIsRootedOrUnc(string rootedEntryKey)
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchiveWithRawEntryKey(
            this.host,
            archivePath,
            rootedEntryKey,
            content: "payload");

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);
        var exception = Assert.Throws<InvalidDataException>(() => source.GetFiles());
        Assert.Contains("archive-relative", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetFiles_Throws_WhenArchiveEntryKeyContainsParentDirectorySegment()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            ("../x.html", "payload"));

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);
        var exception = Assert.Throws<InvalidDataException>(() => source.GetFiles());
        Assert.Contains("..", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetFiles_Throws_WhenArchiveHasDuplicateEntryPathsAfterNormalization()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            (@"Kyiv\a.html", "one"),
            ("Kyiv/a.html", "two"));

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);
        var exception = Assert.Throws<InvalidDataException>(() => source.GetFiles());
        Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FileExists_AcceptsRelativeWindowsSeparators()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.host, "weather.7z");
        SourceFileSystemTestSupport.WriteSevenZipArchive(
            this.host,
            archivePath,
            ("Kyiv/2003-01-01.html", "one"));

        using var source = new SevenZipSourceFileSystem(this.host, archivePath);
        Assert.True(source.FileExists(@"Kyiv\2003-01-01.html"));
    }
}
