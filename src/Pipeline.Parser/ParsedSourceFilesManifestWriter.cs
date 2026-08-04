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

public sealed class ParsedSourceFilesManifestWriter
{
    private readonly ILogger<ParsedSourceFilesManifestWriter> logger;
    private readonly IFileSystem fileSystem;
    private readonly CsvRecordWriter csvRecordWriter;

    public ParsedSourceFilesManifestWriter(
        ILogger<ParsedSourceFilesManifestWriter> logger,
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

    public void Write(IReadOnlyList<ParsedSourceFileEntry> entries, string outputDirectory)
    {
        Argument.ThrowIfNull(entries);
        Argument.ThrowIfNull(outputDirectory);
        var records = entries
            .OrderBy(entry => entry.Place, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Date)
            .Select(entry => new ParsedSourceFileManifestRecord
            {
                Place = entry.Place,
                Date = entry.Date,
                SourceFilePath = entry.SourceFilePath,
            })
            .ToList();

        var rowCount = this.csvRecordWriter.WriteRecords(
            outputDirectory,
            WeatherCsvOutputPaths.ParsedSourceFilesManifestFileName,
            records);
        var csvPath = this.fileSystem.Path.Combine(outputDirectory, WeatherCsvOutputPaths.ParsedSourceFilesManifestFileName);
        this.logger.LogInformation(
            "Wrote manifest {FileName} to {CsvPath} ({RowCount} data rows)",
            WeatherCsvOutputPaths.ParsedSourceFilesManifestFileName,
            csvPath,
            rowCount);
    }
}
