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

internal sealed class CsvTreeSource
{
    private readonly IFileSystem fileSystem;
    private readonly IReadOnlyDictionary<string, CsvTreeEntry> entries;
    private readonly bool isZip;

    private CsvTreeSource(
        IFileSystem fileSystem,
        string path,
        IReadOnlyDictionary<string, CsvTreeEntry> entries,
        bool isZip)
    {
        this.fileSystem = fileSystem;
        this.Path = path;
        this.entries = entries;
        this.isZip = isZip;
        this.Paths = entries.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.DisplayPath,
            StringComparer.OrdinalIgnoreCase);
    }

    public string Path { get; }

    public IReadOnlyDictionary<string, string> Paths { get; }

    public static CsvTreeSource Create(IFileSystem fileSystem, string path)
    {
        var fullPath = fileSystem.Path.GetFullPath(path);
        if (fileSystem.Directory.Exists(fullPath))
        {
            return CreateDirectory(fileSystem, fullPath);
        }

        if (fileSystem.File.Exists(fullPath) && IsZipPath(fileSystem, fullPath))
        {
            return CreateZip(fileSystem, fullPath);
        }

        throw new FileNotFoundException($"not a directory or ZIP file: {fullPath}", fullPath);
    }

    public static bool IsZipPath(IFileSystem fileSystem, string path) =>
        string.Equals(fileSystem.Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Opens a read session that can stream individual CSV entries without loading the whole tree.
    /// For ZIP sources the archive stays open until the session is disposed.
    /// </summary>
    /// <returns>A disposable session for opening individual CSV streams.</returns>
    public CsvTreeReadSession OpenReadSession() =>
        new(this.fileSystem, this.Path, this.entries, this.isZip);

    private static CsvTreeSource CreateDirectory(IFileSystem fileSystem, string path)
    {
        var entries = new Dictionary<string, CsvTreeEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in fileSystem.Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            if (!IsCsvPath(fileSystem, filePath))
            {
                continue;
            }

            var fullFilePath = fileSystem.Path.GetFullPath(filePath);
            var relativePath = ToPosixRelativePath(fileSystem, path, fullFilePath);
            var length = fileSystem.FileInfo.New(fullFilePath).Length;
            var entry = new CsvTreeEntry(fullFilePath, fullFilePath, length);
            if (!entries.TryAdd(relativePath, entry))
            {
                throw new InvalidDataException(
                    $"directory contains duplicate CSV path after normalization: {relativePath}");
            }
        }

        return new CsvTreeSource(fileSystem, path, OrderEntries(entries), isZip: false);
    }

    private static CsvTreeSource CreateZip(IFileSystem fileSystem, string path)
    {
        var entries = new Dictionary<string, CsvTreeEntry>(StringComparer.OrdinalIgnoreCase);
        using (var stream = fileSystem.File.OpenRead(path))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
        {
            var wrapperPrefix = fileSystem.Path.GetFileNameWithoutExtension(path) + "/";

            foreach (var entry in archive.Entries)
            {
                var entryPath = NormalizeZipEntryPath(entry.FullName);
                if (entryPath.EndsWith("/", StringComparison.Ordinal) || !IsCsvPath(fileSystem, entryPath))
                {
                    continue;
                }

                var relativePath = entryPath.StartsWith(wrapperPrefix, StringComparison.OrdinalIgnoreCase)
                    ? entryPath[wrapperPrefix.Length..]
                    : entryPath;
                if (relativePath.Length == 0)
                {
                    continue;
                }

                var length = ReadZipEntryLength(entry);
                var treeEntry = new CsvTreeEntry(
                    entry.FullName,
                    $"{path}!/{entry.FullName}",
                    length);
                if (!entries.TryAdd(relativePath, treeEntry))
                {
                    throw new InvalidDataException(
                        $"ZIP file contains duplicate CSV path after folder normalization: {relativePath}");
                }
            }
        }

        return new CsvTreeSource(fileSystem, path, OrderEntries(entries), isZip: true);
    }

    private static bool IsCsvPath(IFileSystem fileSystem, string path) =>
        string.Equals(fileSystem.Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, CsvTreeEntry> OrderEntries(
        Dictionary<string, CsvTreeEntry> entries) =>
        entries
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    private static string NormalizeZipEntryPath(string fullName)
    {
        var entryPath = fullName.Replace('\\', '/');
        if (IsRootedOrUncZipEntryKey(entryPath))
        {
            throw new InvalidDataException(
                $"ZIP CSV entry path must be archive-relative (not rooted or UNC): {fullName}");
        }

        while (entryPath.StartsWith("./", StringComparison.Ordinal))
        {
            entryPath = entryPath[2..];
        }

        if (string.IsNullOrEmpty(entryPath))
        {
            throw new InvalidDataException($"ZIP CSV entry path is empty after normalization: {fullName}");
        }

        EnsureNoParentDirectorySegments(entryPath, fullName);
        return entryPath;
    }

    private static bool IsRootedOrUncZipEntryKey(string entryKey)
    {
        if (entryKey.StartsWith('/') || entryKey.StartsWith('\\'))
        {
            return true;
        }

        if (entryKey.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        return entryKey.Length >= 2
            && char.IsAsciiLetter(entryKey[0])
            && entryKey[1] == ':';
    }

    private static void EnsureNoParentDirectorySegments(string entryPath, string originalFullName)
    {
        foreach (var segment in entryPath.Split('/'))
        {
            if (segment == "..")
            {
                throw new InvalidDataException(
                    $"ZIP CSV entry path contains parent-directory segment '..': {originalFullName}");
            }
        }
    }

    private static long ReadZipEntryLength(ZipArchiveEntry entry)
    {
        try
        {
            var length = entry.Length;
            if (length < 0)
            {
                throw new InvalidDataException(
                    $"ZIP CSV entry has unknown uncompressed length: {entry.FullName}");
            }

            return length;
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidDataException(
                $"ZIP CSV entry has unknown uncompressed length: {entry.FullName}",
                ex);
        }
    }

    private static string ToPosixRelativePath(IFileSystem fileSystem, string root, string path) =>
        fileSystem.Path.GetRelativePath(root, path).Replace('\\', '/');
}
