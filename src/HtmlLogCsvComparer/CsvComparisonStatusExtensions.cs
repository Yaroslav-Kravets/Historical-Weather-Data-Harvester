// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

using System.Diagnostics;

public static class CsvComparisonStatusExtensions
{
    public static string ToDisplayName(this CsvComparisonStatus status) =>
        status switch
        {
            CsvComparisonStatus.Equal => "EQUAL",
            CsvComparisonStatus.PartlyEqual => "PARTLY EQUAL",
            CsvComparisonStatus.NotEqual => "NOT EQUAL",
            _ => throw new UnreachableException($"Unexpected comparison status: {status}"),
        };
}
