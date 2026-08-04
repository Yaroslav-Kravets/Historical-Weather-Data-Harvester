// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.SourceFileSystem;

using System.IO.Abstractions;
using Common;

public static class SevenZipSourcePath
{
    /// <summary>
    /// Determines whether the source path should be read as a 7z archive.
    /// A directory may legitimately be named <c>*.7z</c>, so an existing directory is never
    /// treated as an archive. A missing <c>*.7z</c> path still resolves to the archive branch
    /// so the caller reports it as a missing archive rather than a missing directory.
    /// </summary>
    /// <param name="fileSystem">The file system hosting the path.</param>
    /// <param name="path">The configured source path.</param>
    /// <returns><see langword="true"/> when the path denotes a 7z archive.</returns>
    public static bool IsSevenZipPath(IFileSystem fileSystem, string path)
    {
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(path);

        return string.Equals(fileSystem.Path.GetExtension(path), ".7z", StringComparison.OrdinalIgnoreCase)
            && !fileSystem.Directory.Exists(path);
    }
}
