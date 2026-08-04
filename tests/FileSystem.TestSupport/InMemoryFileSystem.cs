// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace FileSystem.TestSupport;

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

public static class InMemoryFileSystem
{
    public const string Root = "/data/";

    public static MockFileSystem Create() => new();

    public static string UnderRoot(IFileSystem fileSystem, params string[] parts) =>
        fileSystem.Path.Combine(Root, fileSystem.Path.Combine(parts));
}
