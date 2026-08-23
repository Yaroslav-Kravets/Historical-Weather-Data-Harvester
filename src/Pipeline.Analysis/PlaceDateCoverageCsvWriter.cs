// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis;

using System.IO.Abstractions;
using Common;
using Microsoft.Extensions.Logging;

public sealed class PlaceDateCoverageCsvWriter
{
    private readonly ILogger<PlaceDateCoverageCsvWriter> logger;
    private readonly IFileSystem fileSystem;
    private readonly CsvRecordWriter csvRecordWriter;

    public PlaceDateCoverageCsvWriter(
        ILogger<PlaceDateCoverageCsvWriter> logger,
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

    public void Write(IReadOnlyList<PlaceDateCoverageRow> coverageRows, string stageDirectory)
    {
        Argument.ThrowIfNull(coverageRows);
        Argument.ThrowIfNull(stageDirectory);

        var records = coverageRows
            .Select(row => new PlaceDateCoverageCsvRecord(
                row.Place,
                row.FirstDate,
                row.LastDate,
                row.ObservedDays,
                row.SkippedDays,
                row.SkippedDates))
            .ToList();

        var rowCount = this.csvRecordWriter.WriteRecords(
            stageDirectory,
            WeatherCsvOutputPaths.PlaceDateCoverageFileName,
            records);
        var csvPath = this.fileSystem.Path.Combine(
            stageDirectory,
            WeatherCsvOutputPaths.PlaceDateCoverageFileName);
        this.logger.LogInformation(
            "Wrote place date coverage to {CsvPath} ({RowCount} records)",
            csvPath,
            rowCount);
    }
}
