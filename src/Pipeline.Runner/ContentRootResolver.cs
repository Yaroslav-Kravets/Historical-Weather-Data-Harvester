// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Runner;

using System.IO.Abstractions;
using Common;

public static class ContentRootResolver
{
    public static string Resolve(IFileSystem fileSystem)
    {
        Argument.ThrowIfNull(fileSystem);

        var projectDir = fileSystem.Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "src", "Pipeline.Runner");
        return fileSystem.Directory.Exists(projectDir)
            && fileSystem.File.Exists(fileSystem.Path.Combine(projectDir, "appsettings.json"))
            ? projectDir
            : fileSystem.Directory.GetCurrentDirectory();
    }
}
