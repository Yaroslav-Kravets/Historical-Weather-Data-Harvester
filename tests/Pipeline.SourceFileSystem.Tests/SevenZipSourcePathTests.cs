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
using FileSystem.TestSupport;
using Xunit;

public sealed class SevenZipSourcePathTests
{
    private readonly IFileSystem fileSystem = InMemoryFileSystem.Create();

    [Theory]
    [InlineData("weather.7z", true)]
    [InlineData("weather.7Z", true)]
    [InlineData("/data/Weather.7z", true)]
    [InlineData("/data/weather", false)]
    [InlineData("/data/weather.zip", false)]
    [InlineData("/data/weather.7z.bak", false)]
    public void IsSevenZipPath_DetectsExtensionCaseInsensitively(string path, bool expected)
    {
        Assert.Equal(expected, SevenZipSourcePath.IsSevenZipPath(this.fileSystem, path));
    }

    [Fact]
    public void IsSevenZipPath_ReturnsFalse_WhenPathIsDirectoryNamedLikeArchive()
    {
        var directory = InMemoryFileSystem.UnderRoot(this.fileSystem, "weather.7z");
        this.fileSystem.Directory.CreateDirectory(directory);

        Assert.False(SevenZipSourcePath.IsSevenZipPath(this.fileSystem, directory));
    }

    [Fact]
    public void IsSevenZipPath_ReturnsTrue_WhenPathIsExistingArchiveFile()
    {
        var archive = InMemoryFileSystem.UnderRoot(this.fileSystem, "weather.7z");
        this.fileSystem.Directory.CreateDirectory(InMemoryFileSystem.Root);
        this.fileSystem.File.WriteAllBytes(archive, [0x37, 0x7A]);

        Assert.True(SevenZipSourcePath.IsSevenZipPath(this.fileSystem, archive));
    }

    [Fact]
    public void IsSevenZipPath_ReturnsTrue_WhenArchivePathIsMissing()
    {
        var missing = InMemoryFileSystem.UnderRoot(this.fileSystem, "missing.7z");

        Assert.True(SevenZipSourcePath.IsSevenZipPath(this.fileSystem, missing));
    }
}
