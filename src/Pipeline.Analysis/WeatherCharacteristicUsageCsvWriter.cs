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

public sealed class WeatherCharacteristicUsageCsvWriter
{
    private readonly ILogger<WeatherCharacteristicUsageCsvWriter> logger;
    private readonly IFileSystem fileSystem;
    private readonly CsvRecordWriter csvRecordWriter;

    public WeatherCharacteristicUsageCsvWriter(
        ILogger<WeatherCharacteristicUsageCsvWriter> logger,
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

    public void Write(IReadOnlyList<WeatherCharacteristicUsageRow> usageRows, string stageDirectory)
    {
        Argument.ThrowIfNull(usageRows);
        Argument.ThrowIfNull(stageDirectory);

        var records = usageRows
            .Select(row => new WeatherCharacteristicUsageCsvRecord(
                row.EnglishName,
                row.NameInHtml,
                row.RowCount,
                row.PercentOfRows))
            .ToList();

        var rowCount = this.csvRecordWriter.WriteRecords(
            stageDirectory,
            WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName,
            records);
        var csvPath = this.fileSystem.Path.Combine(
            stageDirectory,
            WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName);
        this.logger.LogInformation(
            "Wrote weather characteristics usage to {CsvPath} ({RowCount} data rows)",
            csvPath,
            rowCount);
    }
}
