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

/// <summary>
/// Opens a directory or .7z archive as an <see cref="ISourceFileSystem"/>.
/// </summary>
public static class SourceFileSystemFactory
{
    public static ISourceFileSystem Open(IFileSystem hostFileSystem, string path)
    {
        Argument.ThrowIfNull(hostFileSystem);
        Argument.ThrowIfNull(path);

        if (SevenZipSourcePath.IsSevenZipPath(hostFileSystem, path))
        {
            return new SevenZipSourceFileSystem(hostFileSystem, path);
        }

        return new DirectorySourceFileSystem(hostFileSystem, path);
    }
}
