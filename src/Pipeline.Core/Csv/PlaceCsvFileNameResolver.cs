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

public sealed class PlaceCsvFileNameResolver
{
    private readonly IFileSystem fileSystem;

    public PlaceCsvFileNameResolver(IFileSystem fileSystem)
    {
        Argument.ThrowIfNull(fileSystem);

        this.fileSystem = fileSystem;
    }

    public string ToCsvFileName(string placeName)
    {
        if (string.IsNullOrWhiteSpace(placeName))
        {
            throw new ArgumentException("Place name is empty or null.", nameof(placeName));
        }

        var fileName = $"{placeName.Trim()}.csv";
        foreach (var invalidChar in this.fileSystem.Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
    }

    public string GetPlaceNameFromCsvFileName(string csvFileName)
    {
        Argument.ThrowIfNull(csvFileName);
        return this.fileSystem.Path.GetFileNameWithoutExtension(csvFileName);
    }
}
