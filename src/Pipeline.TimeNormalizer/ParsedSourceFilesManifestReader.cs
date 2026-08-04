// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer;

using System.Globalization;
using System.IO.Abstractions;
using Common;
using CsvHelper;

public sealed class ParsedSourceFilesManifestReader
{
    private readonly IFileSystem fileSystem;

    public ParsedSourceFilesManifestReader(IFileSystem fileSystem)
    {
        Argument.ThrowIfNull(fileSystem);

        this.fileSystem = fileSystem;
    }

    public Dictionary<string, Dictionary<DateTime, string>> ReadByPlaceAndDate(string parsedStageDirectory)
    {
        Argument.ThrowIfNull(parsedStageDirectory);
        var manifestPath = this.fileSystem.Path.Combine(parsedStageDirectory, WeatherCsvOutputPaths.ParsedSourceFilesManifestFileName);
        if (!this.fileSystem.File.Exists(manifestPath))
        {
            return new Dictionary<string, Dictionary<DateTime, string>>(StringComparer.OrdinalIgnoreCase);
        }

        using var reader = CsvFileStreams.OpenReadStream(this.fileSystem.File, manifestPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var result = new Dictionary<string, Dictionary<DateTime, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in csv.GetRecords<ParsedSourceFileManifestRecord>())
        {
            if (!result.TryGetValue(record.Place, out var byDate))
            {
                byDate = new Dictionary<DateTime, string>();
                result[record.Place] = byDate;
            }

            byDate[record.Date.Date] = record.SourceFilePath;
        }

        return result;
    }

    public string ResolveSourceFilePath(
        IReadOnlyDictionary<string, Dictionary<DateTime, string>> manifestByPlace,
        string place,
        DateTime date)
    {
        Argument.ThrowIfNull(manifestByPlace);
        Argument.ThrowIfNull(place);
        if (manifestByPlace.TryGetValue(place, out var byDate) &&
            byDate.TryGetValue(date.Date, out var sourceFilePath))
        {
            return sourceFilePath;
        }

        return $"{place}/{date:yyyy-MM-dd}";
    }
}
