// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using Common;
using Microsoft.Extensions.Logging;

internal static class PlacePathCheckLogger
{
    public static void LogRunSummary(ILogger logger, ParsingIssueCollector issueCollector)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(issueCollector);

        var totals = issueCollector.GetPlacePathCheckTotals();
        if (totals.FilesChecked == 0)
        {
            return;
        }

        logger.LogInformation(
            "Place path check summary: {FilesChecked} checked, {Matches} matched, {Mismatches} mismatched.",
            totals.FilesChecked,
            totals.Matches,
            totals.Mismatches);
    }

    public static void LogPerPlaceSummary(ILogger logger, ParsingIssueCollector issueCollector)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(issueCollector);

        foreach (var placeSummary in issueCollector.GetPlacePathCheckSummaryByPlace())
        {
            var logLevel = placeSummary.Mismatches > 0 ? LogLevel.Warning : LogLevel.Information;
            logger.Log(
                logLevel,
                "Place path check for {Place}: {Matches} matched, {Mismatches} mismatched ({FilesChecked} files checked).",
                placeSummary.Place,
                placeSummary.Matches,
                placeSummary.Mismatches,
                placeSummary.FilesChecked);
        }
    }
}
