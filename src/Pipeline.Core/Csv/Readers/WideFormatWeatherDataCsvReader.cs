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
/// Reads denormalized weather CSV files from the parsed stage directory (one 0/1 column per characteristic).
/// </summary>
public sealed class DenormalizedWeatherDataCsvReader
{
    private readonly IFileSystem fileSystem;

    public DenormalizedWeatherDataCsvReader(IFileSystem fileSystem)
    {
        Argument.ThrowIfNull(fileSystem);

        this.fileSystem = fileSystem;
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<DateTime, IReadOnlyList<WeatherDataRow>>> ReadAllPlaces(string parsedStageDirectory)
    {
        Argument.ThrowIfNull(parsedStageDirectory);
        if (!this.fileSystem.Directory.Exists(parsedStageDirectory))
        {
            throw new DirectoryNotFoundException($"Parsed stage directory not found: {parsedStageDirectory}");
        }

        var result = new Dictionary<string, IReadOnlyDictionary<DateTime, IReadOnlyList<WeatherDataRow>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var csvPath in CsvDirectoryFiles.EnumerateCsvFiles(this.fileSystem, parsedStageDirectory))
        {
            var fileName = this.fileSystem.Path.GetFileName(csvPath);
            if (WeatherCsvOutputPaths.IsStageRootSidecarCsvFileName(fileName))
            {
                continue;
            }

            var placeName = this.fileSystem.Path.GetFileNameWithoutExtension(csvPath);
            CsvDirectoryFiles.AddPlaceOrThrow(result, placeName, this.ReadPlaceFile(csvPath), csvPath);
        }

        return result;
    }

    public IReadOnlyDictionary<DateTime, IReadOnlyList<WeatherDataRow>> ReadPlaceFile(string csvPath)
    {
        Argument.ThrowIfNull(csvPath);
        using var reader = CsvFileStreams.OpenReadStream(this.fileSystem.File, csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!csv.Read())
        {
            return new SortedDictionary<DateTime, IReadOnlyList<WeatherDataRow>>();
        }

        var header = new string[csv.Parser.Count];
        for (var i = 0; i < csv.Parser.Count; i++)
        {
            header[i] = csv.GetField(i) ?? string.Empty;
        }

        var columnIndexes = BuildColumnIndexes(header);
        var rows = new List<WeatherDataRow>();

        while (csv.Read())
        {
            rows.Add(ParseRow(csv, columnIndexes));
        }

        var grouped = WeatherDataRowsByDateGrouper.Group(rows);
        var result = new SortedDictionary<DateTime, IReadOnlyList<WeatherDataRow>>();
        foreach (var (date, dayRows) in grouped)
        {
            result[date] = dayRows;
        }

        return result;
    }

    public int CountDataRows(string csvPath)
    {
        Argument.ThrowIfNull(csvPath);
        if (!this.fileSystem.File.Exists(csvPath))
        {
            return 0;
        }

        using var reader = CsvFileStreams.OpenReadStream(this.fileSystem.File, csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!csv.Read())
        {
            return 0;
        }

        var count = 0;
        while (csv.Read())
        {
            count++;
        }

        return count;
    }

    private static ColumnIndexes BuildColumnIndexes(IReadOnlyList<string> header)
    {
        var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Count; i++)
        {
            indexes[header[i]] = i;
        }

        int RequireColumn(string columnName)
        {
            if (!indexes.TryGetValue(columnName, out var index))
            {
                throw new InvalidDataException($"Denormalized CSV is missing required column '{columnName}'.");
            }

            return index;
        }

        var characteristicColumnIndexes = new List<(WeatherCharacteristics Flag, int Index)>();
        foreach (var (flag, columnName) in WeatherCharacteristicsColumns.All)
        {
            characteristicColumnIndexes.Add((flag, RequireColumn(columnName)));
        }

        return new ColumnIndexes(
            RequireColumn(WeatherScalarCsvColumns.DateTime),
            RequireColumn(WeatherScalarCsvColumns.Temperature),
            RequireColumn(WeatherScalarCsvColumns.WindDirection),
            RequireColumn(WeatherScalarCsvColumns.WindSpeed),
            RequireColumn(WeatherScalarCsvColumns.AtmosphericPressure),
            RequireColumn(WeatherScalarCsvColumns.Humidity),
            characteristicColumnIndexes);
    }

    private static WeatherDataRow ParseRow(CsvReader csv, ColumnIndexes columnIndexes)
    {
        var time = DateTime.ParseExact(
            csv.GetField(columnIndexes.DateTimeIndex) ?? string.Empty,
            WeatherScalarCsvColumns.DateTimeFormat,
            CultureInfo.InvariantCulture);

        var characteristics = WeatherCharacteristics.None;
        foreach (var (flag, index) in columnIndexes.CharacteristicColumnIndexes)
        {
            var cell = csv.GetField(index) ?? string.Empty;
            if (cell == "1")
            {
                characteristics |= flag;
            }
            else if (cell != "0" && cell.Length > 0)
            {
                throw new InvalidDataException(
                    $"Denormalized CSV characteristic column must be 0 or 1, got '{cell}'.");
            }
        }

        return new WeatherDataRow(
            time,
            characteristics,
            int.Parse(csv.GetField(columnIndexes.TemperatureIndex) ?? string.Empty, CultureInfo.InvariantCulture),
            int.Parse(csv.GetField(columnIndexes.WindDirectionIndex) ?? string.Empty, CultureInfo.InvariantCulture),
            decimal.Parse(csv.GetField(columnIndexes.WindSpeedIndex) ?? string.Empty, CultureInfo.InvariantCulture),
            int.Parse(csv.GetField(columnIndexes.AtmosphericPressureIndex) ?? string.Empty, CultureInfo.InvariantCulture),
            int.Parse(csv.GetField(columnIndexes.HumidityIndex) ?? string.Empty, CultureInfo.InvariantCulture));
    }

    private sealed record ColumnIndexes(
        int DateTimeIndex,
        int TemperatureIndex,
        int WindDirectionIndex,
        int WindSpeedIndex,
        int AtmosphericPressureIndex,
        int HumidityIndex,
        IReadOnlyList<(WeatherCharacteristics Flag, int Index)> CharacteristicColumnIndexes);
}
