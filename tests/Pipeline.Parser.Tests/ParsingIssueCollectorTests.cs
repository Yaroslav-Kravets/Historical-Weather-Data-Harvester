// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser.Tests;

using Xunit;

public sealed class ParsingIssueCollectorTests
{
    private readonly ParsingIssueCollector collector = new(new PlaceConverter());

    [Fact]
    public void AddParseFailure_RecordsPlaceFromFilePath()
    {
        this.collector.AddParseFailure("/mnt/Weather/Real/Kyiv/2003-01-01.html");

        var snapshot = this.collector.GetSnapshot();
        Assert.True(snapshot.ContainsKey("Kyiv"));
        Assert.Equal(1, snapshot["Kyiv"].ParseFailures);
    }

    [Fact]
    public void ConcurrentAdds_AreAllRecorded()
    {
        Parallel.For(0, 100, i =>
        {
            this.collector.AddSkippedFile("Kyiv");
            this.collector.AddDuplicateDate("Kyiv");
        });

        var snapshot = this.collector.GetSnapshot();
        Assert.Equal(100, snapshot["Kyiv"].SkippedFiles);
        Assert.Equal(100, snapshot["Kyiv"].DuplicateDates);
    }

    [Fact]
    public void ConcurrentParseFailures_OnSamePlace_AreAllRecorded()
    {
        Parallel.For(0, 100, i =>
        {
            this.collector.AddParseFailure($"/mnt/Weather/Real/Kyiv/file-{i}.html");
        });

        var snapshot = this.collector.GetSnapshot();
        Assert.Equal(100, snapshot["Kyiv"].ParseFailures);
    }

    [Fact]
    public void AddPathPlaceMismatch_IncrementsPathPlaceMismatchesOnly_NotParseFailures()
    {
        this.collector.AddPathPlaceMismatch("/Real/Kyiv/c.html", "Kyiv", "Харькове", "Kharkiv");

        var snapshot = this.collector.GetSnapshot();
        var kharkivCounts = snapshot["Kharkiv"];
        Assert.Equal(0, kharkivCounts.ParseFailures);
        Assert.Equal(1, kharkivCounts.PathPlaceMismatches);
        Assert.Equal(1, kharkivCounts.TotalIssues);
    }

    [Fact]
    public void GetPathSelfCheckTotals_AggregatesMatchesAndMismatches()
    {
        this.collector.AddPathPlaceMatch("/Real/Kyiv/a.html", "Kyiv", "Киеве", "Kyiv");
        this.collector.AddPathPlaceMatch("/Real/Kyiv/b.html", "Kyiv", "Киеве", "Kyiv");
        this.collector.AddPathPlaceMismatch("/Real/Kyiv/c.html", "Kyiv", "Харькове", "Kharkiv");

        var totals = this.collector.GetPathSelfCheckTotals();

        Assert.Equal(3, totals.FilesChecked);
        Assert.Equal(2, totals.Matches);
        Assert.Equal(1, totals.Mismatches);
    }

    [Fact]
    public void GetPathSelfCheckSummaryByPlace_GroupsByHtmlPlace()
    {
        this.collector.AddPathPlaceMatch("/Real/Kyiv/a.html", "Kyiv", "Киеве", "Kyiv");
        this.collector.AddPathPlaceMismatch("/Real/Kyiv/b.html", "Kyiv", "Харькове", "Kharkiv");
        this.collector.AddPathPlaceMatch("/Real/Kharkiv/c.html", "Kharkiv", "Харькове", "Kharkiv");

        var summaryByPlace = this.collector.GetPathSelfCheckSummaryByPlace();

        Assert.Equal(2, summaryByPlace.Count);

        var kyivSummary = summaryByPlace.Single(summary => summary.Place == "Kyiv");
        Assert.Equal(1, kyivSummary.FilesChecked);
        Assert.Equal(1, kyivSummary.Matches);
        Assert.Equal(0, kyivSummary.Mismatches);

        var kharkivSummary = summaryByPlace.Single(summary => summary.Place == "Kharkiv");
        Assert.Equal(2, kharkivSummary.FilesChecked);
        Assert.Equal(1, kharkivSummary.Matches);
        Assert.Equal(1, kharkivSummary.Mismatches);
    }
}
