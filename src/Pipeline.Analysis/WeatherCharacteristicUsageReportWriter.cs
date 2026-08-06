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
using HtmlLog;
using Microsoft.Extensions.Logging;

public sealed class WeatherCharacteristicUsageReportWriter
{
    private readonly ILogger<WeatherCharacteristicUsageReportWriter> logger;
    private readonly IFileSystem fileSystem;
    private readonly HtmlLogFileManager htmlLogFileManager;

    public WeatherCharacteristicUsageReportWriter(
        ILogger<WeatherCharacteristicUsageReportWriter> logger,
        IFileSystem fileSystem,
        HtmlLogFileManager htmlLogFileManager)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(htmlLogFileManager);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.htmlLogFileManager = htmlLogFileManager;
    }

    public void Write(IReadOnlyList<WeatherCharacteristicUsageRow> usageRows, string htmlReportPath)
    {
        Argument.ThrowIfNull(usageRows);
        Argument.ThrowIfNull(htmlReportPath);

        if (usageRows.Count == 0)
        {
            this.logger.LogWarning("No weather characteristic usage rows to write to HTML report.");
            return;
        }

        var tableRows = usageRows
            .Select(row => new
            {
                row.EnglishName,
                row.NameInHtml,
                row.RowCount,
                row.PercentOfRows,
            })
            .ToList();

        var tableHtml = HtmlLogWriter.RenderTableHtml(tableRows, "Available Weather Characteristics");
        if (tableHtml.Length == 0)
        {
            return;
        }

        if (!this.fileSystem.File.Exists(htmlReportPath))
        {
            using (var htmlWriter = new HtmlLogWriter(
                this.htmlLogFileManager,
                htmlReportPath,
                "Historical Weather Data Harvester — Weather Characteristics"))
            {
                htmlWriter.WriteTable(tableRows, "Available Weather Characteristics");
            }

            this.logger.LogInformation(
                "Wrote weather characteristics HTML report to {ReportPath} ({RowCount} rows)",
                htmlReportPath,
                usageRows.Count);
            return;
        }

        var existingHtml = this.fileSystem.File.ReadAllText(htmlReportPath);
        var footerIndex = existingHtml.IndexOf(HtmlLogWriter.FooterStartMarker, StringComparison.Ordinal);
        if (footerIndex < 0)
        {
            throw new InvalidOperationException(
                $"HTML report '{htmlReportPath}' is missing the expected footer marker; cannot append usage table.");
        }

        var updatedHtml = existingHtml.Insert(footerIndex, tableHtml);
        this.fileSystem.File.WriteAllText(htmlReportPath, updatedHtml);

        this.logger.LogInformation(
            "Appended weather characteristics table to {ReportPath} ({RowCount} rows)",
            htmlReportPath,
            usageRows.Count);
    }
}
