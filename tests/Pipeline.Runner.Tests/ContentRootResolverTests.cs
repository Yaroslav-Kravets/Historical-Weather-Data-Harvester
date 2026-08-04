// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Runner.Tests;

using FileSystem.TestSupport;
using Xunit;

public sealed class ContentRootResolverTests
{
    [Fact]
    public void Resolve_ReturnsPipelineRunnerProjectDir_WhenAppsettingsPresent()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var cwd = InMemoryFileSystem.UnderRoot(fileSystem, "repo");
        var projectDir = fileSystem.Path.Combine(cwd, "src", "Pipeline.Runner");
        fileSystem.Directory.CreateDirectory(projectDir);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(projectDir, "appsettings.json"), "{}");
        fileSystem.Directory.SetCurrentDirectory(cwd);

        var resolved = ContentRootResolver.Resolve(fileSystem);

        Assert.Equal(fileSystem.Path.GetFullPath(projectDir), fileSystem.Path.GetFullPath(resolved));
    }

    [Fact]
    public void Resolve_ReturnsCurrentDirectory_WhenProjectAppsettingsMissing()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var cwd = InMemoryFileSystem.UnderRoot(fileSystem, "elsewhere");
        fileSystem.Directory.CreateDirectory(cwd);
        fileSystem.Directory.SetCurrentDirectory(cwd);

        var resolved = ContentRootResolver.Resolve(fileSystem);

        Assert.Equal(fileSystem.Path.GetFullPath(cwd), fileSystem.Path.GetFullPath(resolved));
    }
}
