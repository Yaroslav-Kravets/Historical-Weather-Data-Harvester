// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Writers;

using System.IO.Abstractions;
using Common;
using Microsoft.Extensions.Logging;

/// <summary>
/// Writes normalized-column weather CSV files to the <c>normalized-columns/</c> subdirectory.
/// Characteristics are stored in a single comma-separated English column.
/// </summary>
public sealed class NormalizedColumnsWeatherDataCsvWriter
{
    private readonly ILogger<NormalizedColumnsWeatherDataCsvWriter> logger;
    private readonly IFileSystem fileSystem;
    private readonly CsvRecordWriter csvRecordWriter;
    private readonly PlaceCsvFileNameResolver placeCsvFileNameResolver;
    private readonly WeatherDataCsvRecordMap weatherDataCsvRecordMap;

    public NormalizedColumnsWeatherDataCsvWriter(
        ILogger<NormalizedColumnsWeatherDataCsvWriter> logger,
        IFileSystem fileSystem,
        CsvRecordWriter csvRecordWriter,
        PlaceCsvFileNameResolver placeCsvFileNameResolver,
        WeatherDataCsvRecordMap weatherDataCsvRecordMap)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(csvRecordWriter);
        Argument.ThrowIfNull(placeCsvFileNameResolver);
        Argument.ThrowIfNull(weatherDataCsvRecordMap);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.csvRecordWriter = csvRecordWriter;
        this.placeCsvFileNameResolver = placeCsvFileNameResolver;
        this.weatherDataCsvRecordMap = weatherDataCsvRecordMap;
    }

    public void WritePlaceRows(
        IReadOnlyDictionary<string, IReadOnlyList<WeatherDataRow>> rowsByPlace,
        string outputDirectory,
        string resultKind)
    {
        Argument.ThrowIfNull(rowsByPlace);
        Argument.ThrowIfNull(outputDirectory);
        Argument.ThrowIfNull(resultKind);
        if (rowsByPlace.Count == 0)
        {
            this.logger.LogWarning("No {ResultKind} results available to write to CSV.", resultKind);
            return;
        }

        foreach (var (placeName, rows) in rowsByPlace)
        {
            if (rows.Count == 0)
            {
                this.logger.LogDebug("Skipping CSV generation for {Place} because it has no rows.", placeName);
                continue;
            }

            var csvFileName = this.placeCsvFileNameResolver.ToCsvFileName(placeName);
            var csvPath = this.fileSystem.Path.Combine(outputDirectory, csvFileName);
            var records = rows.OrderBy(row => row.Time).Select(WeatherDataCsvRecordMapper.ToRecord);

            this.csvRecordWriter.WriteRecords(
                outputDirectory,
                csvFileName,
                records,
                context => context.RegisterClassMap(this.weatherDataCsvRecordMap));

            this.logger.LogInformation("Wrote {ResultKind} CSV for {Place} to {CsvPath}", resultKind, placeName, csvPath);
        }
    }
}
