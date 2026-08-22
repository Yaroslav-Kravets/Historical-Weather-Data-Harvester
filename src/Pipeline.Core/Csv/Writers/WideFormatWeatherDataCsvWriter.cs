// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Writers;

using System.Globalization;
using System.IO.Abstractions;
using Common;
using CsvHelper;

/// <summary>
/// Writes denormalized weather CSV files where each weather characteristic has its own 0/1 column.
/// </summary>
public sealed class DenormalizedWeatherDataCsvWriter
{
    private readonly IFileSystem fileSystem;
    private readonly PlaceCsvFileNameResolver placeCsvFileNameResolver;

    public DenormalizedWeatherDataCsvWriter(
        IFileSystem fileSystem,
        PlaceCsvFileNameResolver placeCsvFileNameResolver)
    {
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(placeCsvFileNameResolver);

        this.fileSystem = fileSystem;
        this.placeCsvFileNameResolver = placeCsvFileNameResolver;
    }

    public int WritePlaceRows(
        string outputDirectory,
        string fileName,
        IReadOnlyList<WeatherDataRow> rows,
        bool includePlaceColumn = false)
    {
        Argument.ThrowIfNull(outputDirectory);
        Argument.ThrowIfNull(fileName);
        Argument.ThrowIfNull(rows);
        this.fileSystem.Directory.CreateDirectory(outputDirectory);

        var csvPath = this.fileSystem.Path.Combine(outputDirectory, fileName);
        var characteristicColumns = WeatherCharacteristicsColumns.All;
        var placeName = includePlaceColumn
            ? this.placeCsvFileNameResolver.GetPlaceNameFromCsvFileName(fileName)
            : null;

        using var writer = CsvFileStreams.OpenWriteStream(this.fileSystem.File, csvPath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        if (includePlaceColumn)
        {
            csv.WriteField(WeatherCsvColumns.Place);
        }

        foreach (var column in WeatherScalarCsvColumns.ScalarColumns)
        {
            csv.WriteField(column);
        }

        foreach (var (_, columnName) in characteristicColumns)
        {
            csv.WriteField(columnName);
        }

        csv.NextRecord();

        foreach (var row in rows)
        {
            if (includePlaceColumn)
            {
                csv.WriteField(placeName);
            }

            csv.WriteField(row.Time.ToString(WeatherScalarCsvColumns.DateTimeFormat, CultureInfo.InvariantCulture));
            csv.WriteField(row.Temperature);
            csv.WriteField(row.WindDirectionAzimuth);
            csv.WriteField(row.WindSpeed);
            csv.WriteField(row.AtmosphericPressure);
            csv.WriteField(row.Humidity);

            foreach (var (flag, _) in characteristicColumns)
            {
                csv.WriteField((row.WeatherCharacteristics & flag) == flag ? 1 : 0);
            }

            csv.NextRecord();
        }

        return rows.Count;
    }
}
