// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

using System.IO.Abstractions;
using System.IO.Compression;
using Common;

/// <summary>
/// Streams individual CSV files from a directory or ZIP without materializing the whole tree.
/// </summary>
internal sealed class CsvTreeReadSession : IDisposable
{
    private readonly IFileSystem fileSystem;
    private readonly IReadOnlyDictionary<string, CsvTreeEntry> entries;
    private readonly bool isZip;
    private readonly Stream? zipFileStream;
    private readonly ZipArchive? archive;
    private bool disposed;

    public CsvTreeReadSession(
        IFileSystem fileSystem,
        string path,
        IReadOnlyDictionary<string, CsvTreeEntry> entries,
        bool isZip)
    {
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(path);
        Argument.ThrowIfNull(entries);

        this.fileSystem = fileSystem;
        this.Path = path;
        this.entries = entries;
        this.isZip = isZip;

        if (isZip)
        {
            this.zipFileStream = fileSystem.File.OpenRead(path);
            this.archive = new ZipArchive(this.zipFileStream, ZipArchiveMode.Read, leaveOpen: true);
        }
    }

    public string Path { get; }

    public int CsvCount => this.entries.Count;

    public IEnumerable<string> RelativePaths => this.entries.Keys;

    public bool Contains(string relativePath) => this.entries.ContainsKey(relativePath);

    public long GetLength(string relativePath) => this.GetEntry(relativePath).Length;

    public string GetDisplayPath(string relativePath) => this.GetEntry(relativePath).DisplayPath;

    public Stream OpenCsv(string relativePath)
    {
        this.ThrowIfDisposed();
        var entry = this.GetEntry(relativePath);
        if (!this.isZip)
        {
            return this.fileSystem.File.OpenRead(entry.OpenKey);
        }

        var zipEntry = this.archive!.GetEntry(entry.OpenKey)
            ?? throw new InvalidDataException(
                $"ZIP CSV entry missing when reopening archive: {entry.OpenKey}");
        return zipEntry.Open();
    }

    public CsvFile LoadFile(string relativePath)
    {
        this.ThrowIfDisposed();
        var entry = this.GetEntry(relativePath);
        using var stream = this.OpenCsv(relativePath);
        return CsvFile.Load(relativePath, entry.DisplayPath, entry.Length, stream);
    }

    public CsvFileMetadata LoadMetadata(string relativePath)
    {
        this.ThrowIfDisposed();
        var entry = this.GetEntry(relativePath);
        using var stream = this.OpenCsv(relativePath);
        return CsvFile.LoadMetadata(relativePath, entry.DisplayPath, entry.Length, stream);
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.archive?.Dispose();
        this.zipFileStream?.Dispose();
    }

    private CsvTreeEntry GetEntry(string relativePath)
    {
        Argument.ThrowIfNull(relativePath);
        if (!this.entries.TryGetValue(relativePath, out var entry))
        {
            throw new KeyNotFoundException($"CSV path not found in tree '{this.Path}': {relativePath}");
        }

        return entry;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
    }
}
