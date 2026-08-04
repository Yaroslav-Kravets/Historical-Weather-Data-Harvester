// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using System.IO.Abstractions;
using Common;
using Microsoft.Extensions.Logging;

public sealed class ParsedStageManifestCsvWriter
{
    private readonly ILogger<ParsedStageManifestCsvWriter> logger;
    private readonly IFileSystem fileSystem;
    private readonly CsvRecordWriter csvRecordWriter;

    public ParsedStageManifestCsvWriter(
        ILogger<ParsedStageManifestCsvWriter> logger,
        IFileSystem fileSystem,
        CsvRecordWriter csvRecordWriter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(csvRecordWriter);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.csvRecordWriter = csvRecordWriter;
    }

    public void WriteParsedPlacesManifest(
        IReadOnlyList<(string EnglishName, string NameInHtml)> parsedPlaces,
        string outputDirectory) =>
        this.WriteManifest(outputDirectory, WeatherCsvOutputPaths.ParsedPlacesManifestFileName, parsedPlaces);

    public void WriteWeatherCharacteristicsManifest(
        IReadOnlyList<(string EnglishName, string NameInHtml)> parsedCharacteristics,
        string outputDirectory) =>
        this.WriteManifest(outputDirectory, WeatherCsvOutputPaths.WeatherCharacteristicsManifestFileName, parsedCharacteristics);

    private void WriteManifest(
        string outputDirectory,
        string fileName,
        IEnumerable<(string EnglishName, string NameInHtml)> rows)
    {
        var records = rows
            .Select(pair => new ManifestCsvRecord(pair.EnglishName, pair.NameInHtml))
            .ToList();
        var rowCount = this.csvRecordWriter.WriteRecords(outputDirectory, fileName, records);
        var csvPath = this.fileSystem.Path.Combine(outputDirectory, fileName);
        this.logger.LogInformation("Wrote manifest {FileName} to {CsvPath} ({RowCount} data rows)", fileName, csvPath, rowCount);
    }
}
