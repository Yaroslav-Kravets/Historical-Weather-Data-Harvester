// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Readers;

using System.Globalization;
using System.IO.Abstractions;
using Common;
using CsvHelper;

/// <summary>
/// Reads normalized-column weather CSV files where characteristics are stored in a single English column.
/// </summary>
public sealed class NormalizedColumnsWeatherDataCsvReader
{
    private readonly IFileSystem fileSystem;
    private readonly WeatherDataCsvRecordMap weatherDataCsvRecordMap;

    public NormalizedColumnsWeatherDataCsvReader(
        IFileSystem fileSystem,
        WeatherDataCsvRecordMap weatherDataCsvRecordMap)
    {
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(weatherDataCsvRecordMap);

        this.fileSystem = fileSystem;
        this.weatherDataCsvRecordMap = weatherDataCsvRecordMap;
    }

    public IReadOnlyDictionary<string, IReadOnlyList<WeatherDataRow>> ReadAllPlaces(string directory)
    {
        Argument.ThrowIfNull(directory);
        if (!this.fileSystem.Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Weather CSV directory not found: {directory}");
        }

        var result = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase);

        foreach (var csvPath in CsvDirectoryFiles.EnumerateCsvFiles(this.fileSystem, directory))
        {
            var placeName = this.fileSystem.Path.GetFileNameWithoutExtension(csvPath);
            CsvDirectoryFiles.AddPlaceOrThrow(result, placeName, this.ReadPlaceFile(csvPath), csvPath);
        }

        return result;
    }

    public IReadOnlyList<WeatherDataRow> ReadPlaceFile(string csvPath)
    {
        Argument.ThrowIfNull(csvPath);
        using var reader = CsvFileStreams.OpenReadStream(this.fileSystem.File, csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap(this.weatherDataCsvRecordMap);

        var rows = new List<WeatherDataRow>();

        foreach (var record in csv.GetRecords<WeatherDataCsvRecord>())
        {
            rows.Add(WeatherDataCsvRecordMapper.ToRow(record));
        }

        return rows;
    }
}
