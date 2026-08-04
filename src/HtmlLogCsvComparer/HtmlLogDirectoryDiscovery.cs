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
using System.Text.RegularExpressions;
using Common;

public sealed partial class HtmlLogDirectoryDiscovery
{
    private readonly IFileSystem fileSystem;

    public HtmlLogDirectoryDiscovery(IFileSystem fileSystem)
    {
        Argument.ThrowIfNull(fileSystem);

        this.fileSystem = fileSystem;
    }

    public IReadOnlyList<string> DiscoverHtmlLogDirs(string root)
    {
        Argument.ThrowIfNull(root);

        if (!this.fileSystem.Directory.Exists(root))
        {
            throw new HtmlLogDiscoveryException($"not a directory: {root}");
        }

        var directories = this.fileSystem.Directory
            .EnumerateDirectories(root, HtmlLogRunDirectory.SearchPattern, SearchOption.AllDirectories)
            .Select(this.NormalizePath);
        var zipFiles = this.fileSystem.Directory
            .EnumerateFiles(root, HtmlLogRunDirectory.ZipSearchPattern, SearchOption.AllDirectories)
            .Where(path => CsvTreeSource.IsZipPath(this.fileSystem, path))
            .Select(this.NormalizePath);

        var candidates = directories
            .Concat(zipFiles)
            .Where(path => HtmlLogDirPattern().IsMatch(this.GetRunName(path)))
            .ToArray();

        this.ThrowOnDuplicateRunName(candidates);

        return candidates
            .OrderBy(this.HtmlLogTimestamp, StringComparer.Ordinal)
            .ThenBy(static path => path, StringComparer.Ordinal)
            .ToArray();
    }

    [GeneratedRegex(HtmlLogRunDirectory.DirectoryNameRegexPattern)]
    private static partial Regex HtmlLogDirPattern();

    private void ThrowOnDuplicateRunName(IReadOnlyList<string> candidates)
    {
        foreach (var group in candidates.GroupBy(this.GetRunName, StringComparer.Ordinal))
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            var paths = string.Join(", ", group.OrderBy(static path => path, StringComparer.Ordinal));
            throw new HtmlLogDiscoveryException(
                $"multiple HtmlLog sources share run identifier '{group.Key}': {paths}. " +
                "Use exactly one folder or one .zip archive per run name.");
        }
    }

    private string NormalizePath(string path)
    {
        var fullPath = this.fileSystem.Path.GetFullPath(path);
        return fullPath.TrimEnd(
            this.fileSystem.Path.DirectorySeparatorChar,
            this.fileSystem.Path.AltDirectorySeparatorChar);
    }

    private string GetRunName(string path)
    {
        var name = this.fileSystem.Path.GetFileName(path);
        return CsvTreeSource.IsZipPath(this.fileSystem, path)
            ? this.fileSystem.Path.GetFileNameWithoutExtension(name)
            : name;
    }

    private string HtmlLogTimestamp(string path)
    {
        var name = this.GetRunName(path);
        return HtmlLogDirPattern().Match(name).Groups["timestamp"].Value;
    }
}
