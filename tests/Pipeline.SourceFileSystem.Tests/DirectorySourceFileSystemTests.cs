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

public sealed class DirectorySourceFileSystemTests
{
    private readonly IFileSystem host = InMemoryFileSystem.Create();

    public DirectorySourceFileSystemTests()
    {
        this.host.Directory.CreateDirectory(InMemoryFileSystem.Root);
    }

    [Fact]
    public void OpenAll_ReadsDirectoryFilesSequentially()
    {
        var root = InMemoryFileSystem.UnderRoot(this.host, "html");
        this.host.Directory.CreateDirectory(root);
        var first = this.host.Path.Combine(root, "a.html");
        var second = this.host.Path.Combine(root, "b.html");
        this.host.File.WriteAllText(first, "one");
        this.host.File.WriteAllText(second, "two");

        using var source = new DirectorySourceFileSystem(this.host, root);

        Assert.True(source.SupportsParallel);
        Assert.Equal(2, source.GetFiles().Count);
        Assert.True(source.FileExists(first));

        var opened = new List<(string Path, string Content)>();
        foreach (var file in source.OpenAll())
        {
            using (file)
            using (var reader = new StreamReader(file.Content, Encoding.UTF8))
            {
                opened.Add((file.Path, reader.ReadToEnd()));
            }
        }

        Assert.Equal(2, opened.Count);
        Assert.Contains(opened, item => item.Path == first && item.Content == "one");
        Assert.Contains(opened, item => item.Path == second && item.Content == "two");
    }

    [Fact]
    public void OpenAll_AllowsMultipleStreamsOpenConcurrently()
    {
        var root = InMemoryFileSystem.UnderRoot(this.host, "html");
        this.host.Directory.CreateDirectory(root);
        var firstPath = this.host.Path.Combine(root, "a.html");
        var secondPath = this.host.Path.Combine(root, "b.html");
        this.host.File.WriteAllText(firstPath, "one");
        this.host.File.WriteAllText(secondPath, "two");

        using var source = new DirectorySourceFileSystem(this.host, root);
        using var enumerator = source.OpenAll().GetEnumerator();

        Assert.True(enumerator.MoveNext());
        var first = enumerator.Current;
        Assert.True(enumerator.MoveNext());
        var second = enumerator.Current;

        try
        {
            using (var firstReader = new StreamReader(first.Content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            using (var secondReader = new StreamReader(second.Content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                Assert.Equal("one", firstReader.ReadToEnd());
                Assert.Equal("two", secondReader.ReadToEnd());
            }
        }
        finally
        {
            first.Dispose();
            second.Dispose();
        }

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Factory_OpensDirectorySource_ForNonArchivePath()
    {
        var root = InMemoryFileSystem.UnderRoot(this.host, "html");
        this.host.Directory.CreateDirectory(root);

        using var source = SourceFileSystemFactory.Open(this.host, root);
        Assert.IsType<DirectorySourceFileSystem>(source);
        Assert.True(source.SupportsParallel);
    }

    [Fact]
    public void Constructor_Throws_WhenDirectoryMissing()
    {
        var missing = InMemoryFileSystem.UnderRoot(this.host, "missing");
        Assert.Throws<DirectoryNotFoundException>(() => new DirectorySourceFileSystem(this.host, missing));
    }

    [Fact]
    public void FileExists_ReturnsFalse_ForPathOutsideRoot()
    {
        var root = InMemoryFileSystem.UnderRoot(this.host, "html");
        this.host.Directory.CreateDirectory(root);
        var inside = this.host.Path.Combine(root, "a.html");
        this.host.File.WriteAllText(inside, "one");

        var outsideRoot = InMemoryFileSystem.UnderRoot(this.host, "other");
        this.host.Directory.CreateDirectory(outsideRoot);
        var outside = this.host.Path.Combine(outsideRoot, "secret.html");
        this.host.File.WriteAllText(outside, "secret");

        using var source = new DirectorySourceFileSystem(this.host, root);

        Assert.True(source.FileExists(inside));
        Assert.True(this.host.File.Exists(outside));
        Assert.False(source.FileExists(outside));
    }

    [Fact]
    public void OpenAll_DefersOpenRead_UntilContentIsAccessed()
    {
        var root = InMemoryFileSystem.UnderRoot(this.host, "html");
        this.host.Directory.CreateDirectory(root);
        var path = this.host.Path.Combine(root, "a.html");
        this.host.File.WriteAllText(path, "one");

        using var source = new DirectorySourceFileSystem(this.host, root);
        using var enumerator = source.OpenAll().GetEnumerator();
        Assert.True(enumerator.MoveNext());
        using var file = enumerator.Current;

        this.host.File.Delete(path);

        Assert.Equal(path, file.Path);
        Assert.ThrowsAny<Exception>(() => _ = file.Content);
    }
}
