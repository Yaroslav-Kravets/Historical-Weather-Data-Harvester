// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis;

using System.Globalization;
using Common;
using HtmlLog;
using Microsoft.Extensions.Logging;

public sealed class WeatherCharacteristicUsageReportWriter
{
    private const string HtmlReportTitle = "Historical Weather Data Harvester — Weather Characteristics Usage";
    private const string UsageTableTitle = "Weather Characteristics Usage";

    private readonly ILogger<WeatherCharacteristicUsageReportWriter> logger;

    public WeatherCharacteristicUsageReportWriter(
        ILogger<WeatherCharacteristicUsageReportWriter> logger)
    {
        Argument.ThrowIfNull(logger);

        this.logger = logger;
    }

    public void Write(
        IReadOnlyList<WeatherCharacteristicUsageRow> usageRows,
        HtmlLogFileManager htmlLogFileManager,
        string htmlReportPath)
    {
        Argument.ThrowIfNull(usageRows);
        Argument.ThrowIfNull(htmlLogFileManager);
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
                PercentOfRows = FormatPercent(row.PercentOfRows),
            })
            .ToList();

        using var htmlWriter = new HtmlLogWriter(htmlLogFileManager, htmlReportPath, HtmlReportTitle);
        htmlWriter.WriteTable(tableRows, UsageTableTitle);

        this.logger.LogInformation(
            "Wrote weather characteristics usage table ({RowCount} rows)",
            usageRows.Count);
    }

    private static string FormatPercent(double percentOfRows) =>
        percentOfRows.ToString("F5", CultureInfo.InvariantCulture) + "%";
}
