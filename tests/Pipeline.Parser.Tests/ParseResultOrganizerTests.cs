// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class ParseResultOrganizerTests
{
    private readonly PlaceConverter placeConverter = new();
    private readonly ParseResultOrganizer organizer;

    public ParseResultOrganizerTests()
    {
        this.organizer = new ParseResultOrganizer(
            NullLogger<ParseResultOrganizer>.Instance,
            this.placeConverter);
    }

    [Fact]
    public void OrganizeByPlaceAndDate_AcceptsFile_WhenPathPlaceMatchesHtmlCity()
    {
        var collector = new ParsingIssueCollector(this.placeConverter);
        var parseResults = new[]
        {
            ("/data/weather/Real/Kiev/2003-01-01.html", new HtmlParseResult("Киеве", "2003-01-01")),
        };

        var result = this.organizer.OrganizeByPlaceAndDate(parseResults, collector);

        Assert.Equal(0, result.PathPlaceMismatchRejections);
        Assert.Single(result.ResultsByPlace);
        Assert.True(result.ResultsByPlace.ContainsKey("Kyiv"));
        Assert.Single(result.ResultsByPlace["Kyiv"]);
        Assert.Equal(1, collector.GetPathSelfCheckTotals().Matches);
        Assert.Equal(0, collector.GetPathSelfCheckTotals().Mismatches);
    }

    [Fact]
    public void OrganizeByPlaceAndDate_RejectsFile_WhenPathPlaceDoesNotMatchHtmlCity()
    {
        var collector = new ParsingIssueCollector(this.placeConverter);
        var parseResults = new[]
        {
            ("/data/weather/Real/Kiev/2003-01-01.html", new HtmlParseResult("Харькове", "2003-01-01")),
        };

        var result = this.organizer.OrganizeByPlaceAndDate(parseResults, collector);

        Assert.Equal(1, result.PathPlaceMismatchRejections);
        Assert.Empty(result.ResultsByPlace);
        Assert.Empty(result.SourceFileEntries);

        var snapshot = collector.GetSnapshot();
        Assert.Equal(1, snapshot["Kharkiv"].PathPlaceMismatches);
        Assert.Equal(1, snapshot["Kharkiv"].TotalIssues);
        Assert.Equal(0, collector.GetPathSelfCheckTotals().Matches);
        Assert.Equal(1, collector.GetPathSelfCheckTotals().Mismatches);
    }

    [Fact]
    public void OrganizeByPlaceAndDate_RejectsFile_WhenPathHasNoKnownPlaceSegmentButHtmlCityIsKnown()
    {
        var collector = new ParsingIssueCollector(this.placeConverter);
        var parseResults = new[]
        {
            ("/tmp/no-place-here/2003-01-01.html", new HtmlParseResult("Киеве", "2003-01-01")),
        };

        var result = this.organizer.OrganizeByPlaceAndDate(parseResults, collector);

        Assert.Equal(1, result.PathPlaceMismatchRejections);
        Assert.Empty(result.ResultsByPlace);
        Assert.Equal(1, collector.GetSnapshot()["Kyiv"].PathPlaceMismatches);
        Assert.Equal(1, collector.GetPathSelfCheckTotals().Mismatches);
    }

    [Fact]
    public void OrganizeByPlaceAndDate_DuplicateDate_KeepsLastFilePathLexicographically()
    {
        var collector = new ParsingIssueCollector(this.placeConverter);
        const string date = "2003-01-01";
        var parseResults = new[]
        {
            ("/data/weather/Real/Kiev/2003-01-01-z.html", new HtmlParseResult("Киеве", date)),
            ("/data/weather/Real/Kiev/2003-01-01-a.html", new HtmlParseResult("Киеве", date)),
        };

        var result = this.organizer.OrganizeByPlaceAndDate(parseResults, collector);

        var parsedDate = DateTime.ParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var winningEntry = result.ResultsByPlace["Kyiv"][parsedDate];

        Assert.EndsWith("2003-01-01-z.html", winningEntry.FilePath);
        Assert.Single(result.SourceFileEntries);
        Assert.Equal("/data/weather/Real/Kiev/2003-01-01-z.html", result.SourceFileEntries[0].SourceFilePath);
        Assert.Equal(1, collector.GetSnapshot()["Kyiv"].DuplicateDates);
    }

    [Fact]
    public void OrganizeByPlaceAndDate_Throws_WhenHtmlCityNameIsUnknownAndPathHasNoKnownPlace()
    {
        var collector = new ParsingIssueCollector(this.placeConverter);
        var parseResults = new[]
        {
            ("/tmp/no-place-here/2003-01-01.html", new HtmlParseResult("Неизвестное Место", "2003-01-01")),
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => this.organizer.OrganizeByPlaceAndDate(parseResults, collector));

        Assert.Contains("Unknown place", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Неизвестное Место", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OrganizeByPlaceAndDate_Throws_WhenKnownPathHasUnknownHtmlCity()
    {
        var collector = new ParsingIssueCollector(this.placeConverter);
        var parseResults = new[]
        {
            ("/data/weather/Real/Kiev/2003-01-01.html", new HtmlParseResult("Неизвестное Место", "2003-01-01")),
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => this.organizer.OrganizeByPlaceAndDate(parseResults, collector));

        Assert.Contains("Unknown place", exception.Message, StringComparison.Ordinal);
        Assert.Contains("/data/weather/Real/Kiev/2003-01-01.html", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/data/weather/Real/NotAConfiguredPlace/2003-01-01.html")]
    [InlineData("/tmp/misc/archive/2003-01-01.html")]
    public void OrganizeByPlaceAndDate_RejectsFile_WhenUnknownPathFolderHasKnownHtmlCity(string filePath)
    {
        var collector = new ParsingIssueCollector(this.placeConverter);
        var parseResults = new[]
        {
            (filePath, new HtmlParseResult("Киеве", "2003-01-01")),
        };

        var result = this.organizer.OrganizeByPlaceAndDate(parseResults, collector);

        Assert.Equal(1, result.PathPlaceMismatchRejections);
        Assert.Empty(result.ResultsByPlace);
    }
}
