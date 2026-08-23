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

public sealed class AnalysisReportWriter
{
    private const string HtmlReportTitle = "Historical Weather Data Harvester — Analysis";
    private const string PlaceDateCoverageTableTitle = "Place Date Coverage";
    private const string UsageTableTitle = "Weather Characteristics Usage";

    private readonly ILogger<AnalysisReportWriter> logger;

    public AnalysisReportWriter(ILogger<AnalysisReportWriter> logger)
    {
        Argument.ThrowIfNull(logger);

        this.logger = logger;
    }

    public void Write(
        IReadOnlyList<PlaceDateCoverageRow> coverageRows,
        IReadOnlyList<WeatherCharacteristicUsageRow> usageRows,
        HtmlLogFileManager htmlLogFileManager,
        string htmlReportPath)
    {
        Argument.ThrowIfNull(coverageRows);
        Argument.ThrowIfNull(usageRows);
        Argument.ThrowIfNull(htmlLogFileManager);
        Argument.ThrowIfNull(htmlReportPath);

        if (coverageRows.Count == 0 && usageRows.Count == 0)
        {
            this.logger.LogWarning("No analysis rows to write to HTML report.");
            return;
        }

        using var htmlWriter = new HtmlLogWriter(htmlLogFileManager, htmlReportPath, HtmlReportTitle);

        if (coverageRows.Count > 0)
        {
            var coverageTableRows = coverageRows
                .Select(row => new
                {
                    row.Place,
                    row.FirstDate,
                    row.LastDate,
                    row.ObservedDays,
                    row.SkippedDays,
                    row.SkippedDates,
                })
                .ToList();

            htmlWriter.WriteTable(coverageTableRows, PlaceDateCoverageTableTitle);

            this.logger.LogInformation(
                "Wrote place date coverage table ({RowCount} rows)",
                coverageRows.Count);
        }

        if (usageRows.Count > 0)
        {
            var usageTableRows = usageRows
                .Select(row => new
                {
                    row.EnglishName,
                    row.NameInHtml,
                    row.RowCount,
                    PercentOfRows = FormatPercent(row.PercentOfRows),
                })
                .ToList();

            htmlWriter.WriteTable(usageTableRows, UsageTableTitle);

            this.logger.LogInformation(
                "Wrote weather characteristics usage table ({RowCount} rows)",
                usageRows.Count);
        }
    }

    private static string FormatPercent(double percentOfRows) =>
        percentOfRows.ToString("F5", CultureInfo.InvariantCulture) + "%";
}
