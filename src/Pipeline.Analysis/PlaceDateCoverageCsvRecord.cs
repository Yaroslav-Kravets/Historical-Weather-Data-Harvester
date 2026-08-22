// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis;

using CsvHelper.Configuration.Attributes;

public sealed class PlaceDateCoverageCsvRecord
{
    public PlaceDateCoverageCsvRecord()
    {
    }

    public PlaceDateCoverageCsvRecord(
        string place,
        string? firstDate,
        string? lastDate,
        int skippedDays)
    {
        this.Place = place;
        this.FirstDate = firstDate ?? string.Empty;
        this.LastDate = lastDate ?? string.Empty;
        this.SkippedDays = skippedDays;
    }

    [Name("Place")]
    public string Place { get; init; } = string.Empty;

    [Name("FirstDate")]
    public string FirstDate { get; init; } = string.Empty;

    [Name("LastDate")]
    public string LastDate { get; init; } = string.Empty;

    [Name("SkippedDays")]
    public int SkippedDays { get; init; }
}
