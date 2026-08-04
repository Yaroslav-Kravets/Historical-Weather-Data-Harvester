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
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

/// <summary>
/// Sequential <see cref="ISourceFileSystem"/> over a .7z archive.
/// Use <see cref="OpenAll"/>; only one <see cref="SourceFile"/> may be open at a time (solid archives are single-pass).
/// </summary>
/// <remarks>
/// Trust model: archives are assumed trusted (internal weather dumps). There is
/// currently no uncompressed entry-size cap; see the TODO on <see cref="OpenAll"/>.
/// </remarks>
public sealed class SevenZipSourceFileSystem : ISourceFileSystem
{
    private readonly IFileSystem hostFileSystem;
    private readonly string archivePath;
    private IReadOnlyList<string>? entryPaths;
    private HashSet<string>? entryPathSet;
    private Stream? archiveStream;
    private IArchive? archive;
    private IReader? reader;
    private string? outstandingEntryPath;
    private bool openAllInProgress;
    private bool disposed;

    public SevenZipSourceFileSystem(IFileSystem hostFileSystem, string archivePath)
    {
        Argument.ThrowIfNull(hostFileSystem);
        Argument.ThrowIfNull(archivePath);

        this.hostFileSystem = hostFileSystem;
        this.archivePath = hostFileSystem.Path.GetFullPath(archivePath);
    }

    public bool SupportsParallel => false;

    public IReadOnlyList<string> GetFiles()
    {
        this.ThrowIfDisposed();
        this.EnsureEntryIndex();
        return this.entryPaths!.ToArray();
    }

    public bool FileExists(string path)
    {
        this.ThrowIfDisposed();
        Argument.ThrowIfNull(path);

        this.EnsureEntryIndex();
        return this.entryPathSet!.Contains(NormalizeEntryPath(path));
    }

    /// <summary>
    /// Yields each non-directory entry in archive order. Dispose each returned
    /// <see cref="SourceFile"/> before advancing; SharpCompress requires the previous
    /// entry stream to be fully consumed or disposed before the next MoveToNextEntry.
    /// Only one <see cref="OpenAll"/> enumeration may be active at a time; after that
    /// pass completes (or its enumerator is disposed), another pass may be started.
    /// </summary>
    /// <returns>An enumerable of opened archive entries in archive order.</returns>
    public IEnumerable<SourceFile> OpenAll()
    {
        this.ThrowIfDisposed();
        this.EnsureEntryIndex();

        if (this.outstandingEntryPath is not null)
        {
            throw new InvalidOperationException(
                $"Previous archive entry stream for '{this.outstandingEntryPath}' must be disposed " +
                "before enumerating further.");
        }

        if (this.openAllInProgress)
        {
            throw new InvalidOperationException(
                "Another OpenAll enumeration is already in progress; finish or dispose it before starting another.");
        }

        this.openAllInProgress = true;
        try
        {
            this.ResetReader();

            while (true)
            {
                this.ThrowIfDisposed();

                if (this.outstandingEntryPath is not null)
                {
                    throw new InvalidOperationException(
                        $"Previous archive entry stream for '{this.outstandingEntryPath}' must be disposed " +
                        "before opening the next entry.");
                }

                if (!this.reader!.MoveToNextEntry())
                {
                    yield break;
                }

                if (this.reader.Entry.IsDirectory)
                {
                    continue;
                }

                var currentPath = NormalizeEntryPath(this.reader.Entry.Key);
                if (!this.entryPathSet!.Contains(currentPath))
                {
                    throw new InvalidOperationException(
                        $"Archive entry '{currentPath}' was not present in the entry index.");
                }

                // TODO: Enforce a max uncompressed entry size (and/or total bytes read)
                // when opening streams. Non-seekable entry bodies are often fully buffered
                // by consumers (e.g. MemoryStream), so without a cap this package is
                // zip-bomb-friendly if used on untrusted archives. Acceptable today for
                // trusted internal weather dumps; fix before exposing as a general-purpose API.
                // Reserve the outstanding path before yield so MoveNext without Dispose still
                // fails; defer OpenEntryStream until Content is accessed (inside the parse try).
                this.outstandingEntryPath = currentPath;
                var entryPath = currentPath;
                yield return new SourceFile(
                    entryPath,
                    () => this.reader!.OpenEntryStream(),
                    this.OnEntryStreamDisposed);
            }
        }
        finally
        {
            this.openAllInProgress = false;
        }
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.reader?.Dispose();
        this.archive?.Dispose();
        this.archiveStream?.Dispose();
    }

    // TODO: Canonicalize archive entry paths (resolve '.' segments, fuller
    // unsafe-path policy). For now fail closed on rooted/UNC/drive keys,
    // parent-directory segments, and empty keys after slash normalization.
    private static string NormalizeEntryPath(string? entryKey)
    {
        if (string.IsNullOrWhiteSpace(entryKey))
        {
            throw new InvalidDataException("Archive entry path is empty.");
        }

        if (IsRootedOrUncArchiveKey(entryKey))
        {
            throw new InvalidDataException(
                $"Archive entry path must be archive-relative (not rooted or UNC): {entryKey}");
        }

        var normalized = entryKey.Replace('\\', '/');
        if (string.IsNullOrEmpty(normalized))
        {
            throw new InvalidDataException($"Archive entry path is empty after normalization: {entryKey}");
        }

        foreach (var segment in normalized.Split('/'))
        {
            if (segment == "..")
            {
                throw new InvalidDataException(
                    $"Archive entry path contains parent-directory segment '..': {entryKey}");
            }
        }

        return normalized;
    }

    private static bool IsRootedOrUncArchiveKey(string entryKey)
    {
        if (entryKey.StartsWith('/') || entryKey.StartsWith('\\'))
        {
            return true;
        }

        return entryKey.Length >= 2
            && char.IsAsciiLetter(entryKey[0])
            && entryKey[1] == ':';
    }

    private void OnEntryStreamDisposed()
    {
        this.outstandingEntryPath = null;
    }

    private void EnsureEntryIndex()
    {
        if (this.entryPaths is not null)
        {
            return;
        }

        if (!this.hostFileSystem.File.Exists(this.archivePath))
        {
            throw new FileNotFoundException($"Input 7z archive not found: {this.archivePath}", this.archivePath);
        }

        using var stream = this.hostFileSystem.File.OpenRead(this.archivePath);
        using var archiveForIndex = SevenZipArchive.OpenArchive(
            stream,
            new ReaderOptions { LeaveStreamOpen = true });

        var paths = new List<string>();

        // TODO: Decide whether duplicate normalized paths should be merged or
        // handled with richer canonicalization; for now fail closed.
        var pathSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in archiveForIndex.Entries)
        {
            if (entry.IsDirectory)
            {
                continue;
            }

            var normalizedPath = NormalizeEntryPath(entry.Key);
            if (!pathSet.Add(normalizedPath))
            {
                throw new InvalidDataException(
                    $"Archive contains duplicate entry path after normalization: {normalizedPath}");
            }

            paths.Add(normalizedPath);
        }

        this.entryPaths = paths.ToArray();
        this.entryPathSet = pathSet;
    }

    private void ResetReader()
    {
        this.reader?.Dispose();
        this.archive?.Dispose();
        this.archiveStream?.Dispose();
        this.reader = null;
        this.archive = null;
        this.archiveStream = null;

        this.archiveStream = this.hostFileSystem.File.OpenRead(this.archivePath);
        this.archive = SevenZipArchive.OpenArchive(
            this.archiveStream,
            new ReaderOptions { LeaveStreamOpen = true });
        this.reader = this.archive.ExtractAllEntries();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
    }
}
