// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.SourceFileSystem;

/// <summary>
/// Minimal read-only view over a set of input files (directory, in-memory directory, or archive).
/// Files are opened via <see cref="OpenAll"/>. When <see cref="SupportsParallel"/> is
/// <see langword="false"/>, dispose each <see cref="SourceFile"/> before advancing the enumerator.
/// </summary>
public interface ISourceFileSystem : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether multiple <see cref="SourceFile"/> streams from
    /// <see cref="OpenAll"/> may be open at the same time.
    /// Directory sources return <see langword="true"/>; solid .7z sources return <see langword="false"/>.
    /// </summary>
    bool SupportsParallel { get; }

    /// <summary>
    /// Gets all readable file paths under this source (host paths or archive-relative keys).
    /// </summary>
    /// <returns>The file paths available from this source.</returns>
    IReadOnlyList<string> GetFiles();

    /// <summary>
    /// Determines whether the given path exists as a readable file in this source.
    /// </summary>
    /// <param name="path">The host path or archive-relative entry key.</param>
    /// <returns><see langword="true"/> when the file exists; otherwise <see langword="false"/>.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Opens every file. Order is implementation-defined (directory enumeration vs archive order).
    /// When <see cref="SupportsParallel"/> is <see langword="false"/>, dispose each
    /// <see cref="SourceFile"/> before advancing the enumerator, and only one
    /// <see cref="OpenAll"/> enumeration may be active at a time; after that pass
    /// finishes, another pass may be started.
    /// </summary>
    /// <returns>An enumerable of opened source files.</returns>
    IEnumerable<SourceFile> OpenAll();
}
