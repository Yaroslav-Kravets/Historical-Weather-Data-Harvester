// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv;

using System.IO.Abstractions;
using Common;

public static class CsvDirectoryFiles
{
    public static IEnumerable<string> EnumerateCsvFiles(IFileSystem fileSystem, string directory)
    {
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(directory);

        foreach (var filePath in fileSystem.Directory.EnumerateFiles(directory))
        {
            if (IsCsvPath(fileSystem, filePath))
            {
                yield return filePath;
            }
        }
    }

    /// <summary>
    /// Adds a place CSV under a case-insensitive place key, or throws when another file
    /// already maps to the same place name (e.g. <c>Kyiv.csv</c> and <c>kyiv.CSV</c>).
    /// </summary>
    public static void AddPlaceOrThrow<T>(
        IDictionary<string, T> placesByName,
        string placeName,
        T value,
        string csvPath)
    {
        Argument.ThrowIfNull(placesByName);
        Argument.ThrowIfNull(placeName);
        Argument.ThrowIfNull(csvPath);

        if (!placesByName.TryAdd(placeName, value))
        {
            throw new InvalidDataException(
                $"directory contains duplicate place CSV after case-insensitive name normalization: " +
                $"'{placeName}' ({csvPath})");
        }
    }

    private static bool IsCsvPath(IFileSystem fileSystem, string path) =>
        string.Equals(fileSystem.Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase);
}
