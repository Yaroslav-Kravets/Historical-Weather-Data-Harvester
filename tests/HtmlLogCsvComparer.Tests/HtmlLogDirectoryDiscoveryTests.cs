// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer.Tests;

using Common;
using FileSystem.TestSupport;
using Xunit;

public sealed class HtmlLogDirectoryDiscoveryTests
{
    [Fact]
    public void DiscoverHtmlLogDirs_FindsMatchingDirectoriesOnInjectedFileSystem()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var first = fileSystem.Path.Combine(root, HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5)));
        var second = fileSystem.Path.Combine(root, HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6)));
        var ignored = fileSystem.Path.Combine(root, "not-a-html-log");
        fileSystem.Directory.CreateDirectory(first);
        fileSystem.Directory.CreateDirectory(second);
        fileSystem.Directory.CreateDirectory(ignored);

        var discovery = new HtmlLogDirectoryDiscovery(fileSystem);
        var dirs = discovery.DiscoverHtmlLogDirs(root);

        Assert.Equal(2, dirs.Count);
        Assert.Equal(fileSystem.Path.GetFullPath(first), dirs[0]);
        Assert.Equal(fileSystem.Path.GetFullPath(second), dirs[1]);
    }

    [Fact]
    public void DiscoverHtmlLogDirs_FindsZipArchivesWithExplicitZipPattern()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var first = fileSystem.Path.Combine(
            root,
            HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5)) + ".zip");
        var second = fileSystem.Path.Combine(
            root,
            HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6)) + ".zip");
        var ignored = fileSystem.Path.Combine(root, "notes.zip");
        fileSystem.Directory.CreateDirectory(root);
        fileSystem.File.WriteAllBytes(first, []);
        fileSystem.File.WriteAllBytes(second, []);
        fileSystem.File.WriteAllBytes(ignored, []);

        var discovery = new HtmlLogDirectoryDiscovery(fileSystem);
        var sources = discovery.DiscoverHtmlLogDirs(root);

        Assert.Equal(
            [fileSystem.Path.GetFullPath(first), fileSystem.Path.GetFullPath(second)],
            sources);
    }

    [Fact]
    public void DiscoverHtmlLogDirs_ThrowsWhenRootMissing()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var missing = InMemoryFileSystem.UnderRoot(fileSystem, "missing");
        var discovery = new HtmlLogDirectoryDiscovery(fileSystem);

        Assert.Throws<HtmlLogDiscoveryException>(() => discovery.DiscoverHtmlLogDirs(missing));
    }

    [Fact]
    public void DiscoverHtmlLogDirs_OrdersMixedDirectoriesAndZipFilesByTimestamp()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var first = fileSystem.Path.Combine(root, HtmlLogRunDirectory.FormatDirectoryName(
            new DateTime(2026, 1, 2, 3, 4, 5)));
        var second = fileSystem.Path.Combine(
            root,
            HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6)) + ".zip");
        var third = fileSystem.Path.Combine(root, HtmlLogRunDirectory.FormatDirectoryName(
            new DateTime(2026, 1, 2, 3, 4, 7)));
        fileSystem.Directory.CreateDirectory(first);
        fileSystem.File.WriteAllBytes(second, []);
        fileSystem.Directory.CreateDirectory(third);

        var discovery = new HtmlLogDirectoryDiscovery(fileSystem);
        var sources = discovery.DiscoverHtmlLogDirs(root);

        Assert.Equal(
            [fileSystem.Path.GetFullPath(first), fileSystem.Path.GetFullPath(second), fileSystem.Path.GetFullPath(third)],
            sources);
    }

    [Fact]
    public void DiscoverHtmlLogDirs_ThrowsWhenDirectoryAndZipShareRunName()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var runName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var directory = fileSystem.Path.Combine(root, runName);
        var zip = fileSystem.Path.Combine(root, runName + ".zip");
        fileSystem.Directory.CreateDirectory(directory);
        fileSystem.File.WriteAllBytes(zip, []);

        var discovery = new HtmlLogDirectoryDiscovery(fileSystem);

        var exception = Assert.Throws<HtmlLogDiscoveryException>(() => discovery.DiscoverHtmlLogDirs(root));
        Assert.Contains(runName, exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one folder or one .zip", exception.Message, StringComparison.Ordinal);
    }
}
