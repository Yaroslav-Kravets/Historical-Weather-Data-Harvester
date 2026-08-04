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
/// <see cref="ISourceFileSystem"/> over a host directory (real disk or in-memory <see cref="IFileSystem"/>).
/// <see cref="SupportsParallel"/> is <see langword="true"/>; multiple <see cref="SourceFile"/> streams may be open at once.
/// </summary>
public sealed class DirectorySourceFileSystem : ISourceFileSystem
{
    private readonly IFileSystem hostFileSystem;
    private readonly string rootDirectory;
    private bool disposed;

    public DirectorySourceFileSystem(IFileSystem hostFileSystem, string rootDirectory)
    {
        Argument.ThrowIfNull(hostFileSystem);
        Argument.ThrowIfNull(rootDirectory);

        this.hostFileSystem = hostFileSystem;
        this.rootDirectory = hostFileSystem.Path.GetFullPath(rootDirectory);

        if (!hostFileSystem.Directory.Exists(this.rootDirectory))
        {
            throw new DirectoryNotFoundException($"Input directory not found: {this.rootDirectory}");
        }
    }

    public bool SupportsParallel => true;

    public IReadOnlyList<string> GetFiles()
    {
        this.ThrowIfDisposed();
        return this.hostFileSystem.Directory.GetFiles(this.rootDirectory, "*", SearchOption.AllDirectories);
    }

    public bool FileExists(string path)
    {
        this.ThrowIfDisposed();
        Argument.ThrowIfNull(path);

        var fullPath = this.hostFileSystem.Path.GetFullPath(path);
        var relativePath = this.hostFileSystem.Path.GetRelativePath(this.rootDirectory, fullPath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal)
            || this.hostFileSystem.Path.IsPathRooted(relativePath))
        {
            return false;
        }

        return this.hostFileSystem.File.Exists(fullPath);
    }

    public IEnumerable<SourceFile> OpenAll()
    {
        this.ThrowIfDisposed();

        foreach (var path in this.GetFiles())
        {
            this.ThrowIfDisposed();
            var filePath = path;
            yield return new SourceFile(filePath, () => this.hostFileSystem.File.OpenRead(filePath));
        }
    }

    public void Dispose()
    {
        this.disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
    }
}
