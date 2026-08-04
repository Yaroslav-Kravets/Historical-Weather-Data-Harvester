// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using Microsoft.Extensions.Logging;

internal static class PlacePathSelfCheckLogger
{
    public static void LogRunSummary(ILogger logger, ParsingIssueCollector issueCollector)
    {
        var totals = issueCollector.GetPathSelfCheckTotals();
        if (totals.FilesChecked == 0)
        {
            return;
        }

        logger.LogInformation(
            "Place path self-check summary: {FilesChecked} checked, {Matches} matched, {Mismatches} mismatched.",
            totals.FilesChecked,
            totals.Matches,
            totals.Mismatches);
    }

    public static void LogPerPlaceSummary(ILogger logger, ParsingIssueCollector issueCollector)
    {
        foreach (var placeSummary in issueCollector.GetPathSelfCheckSummaryByPlace())
        {
            if (placeSummary.Mismatches > 0)
            {
                logger.LogWarning(
                    "Place path self-check for {Place}: {Matches} matched, {Mismatches} mismatched ({FilesChecked} files checked).",
                    placeSummary.Place,
                    placeSummary.Matches,
                    placeSummary.Mismatches,
                    placeSummary.FilesChecked);
            }
            else
            {
                logger.LogInformation(
                    "Place path self-check for {Place}: {Matches} matched, {Mismatches} mismatched ({FilesChecked} files checked).",
                    placeSummary.Place,
                    placeSummary.Matches,
                    placeSummary.Mismatches,
                    placeSummary.FilesChecked);
            }
        }
    }
}
