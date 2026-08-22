// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis.Tests;

using Xunit;

public sealed class PlaceDateCoverageAggregatorTests
{
    private readonly PlaceDateCoverageAggregator aggregator =
        new(new DateRangeClusterFormatter());

    [Fact]
    public void Aggregate_ContiguousRange_HasZeroSkippedDays()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] =
            [
                CreateRow(new DateTime(2003, 1, 1)),
                CreateRow(new DateTime(2003, 1, 2)),
                CreateRow(new DateTime(2003, 1, 3)),
            ],
        };

        var coverageRows = this.aggregator.Aggregate(rowsByPlace);

        var kyiv = Assert.Single(coverageRows);
        Assert.Equal("Kyiv", kyiv.Place);
        Assert.Equal("2003-01-01", kyiv.FirstDate);
        Assert.Equal("2003-01-03", kyiv.LastDate);
        Assert.Equal(0, kyiv.SkippedDays);
        Assert.Equal(string.Empty, kyiv.SkippedDates);
    }

    [Fact]
    public void Aggregate_GapInRange_CountsSkippedDaysAndListsDates()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] =
            [
                CreateRow(new DateTime(2003, 1, 1)),
                CreateRow(new DateTime(2003, 1, 3)),
            ],
        };

        var coverageRows = this.aggregator.Aggregate(rowsByPlace);

        var kyiv = Assert.Single(coverageRows);
        Assert.Equal("2003-01-01", kyiv.FirstDate);
        Assert.Equal("2003-01-03", kyiv.LastDate);
        Assert.Equal(1, kyiv.SkippedDays);
        Assert.Equal("2003-01-02", kyiv.SkippedDates);
    }

    [Fact]
    public void Aggregate_ConsecutiveGaps_ClustersSkippedDates()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] =
            [
                CreateRow(new DateTime(2003, 1, 1)),
                CreateRow(new DateTime(2003, 1, 5)),
            ],
        };

        var coverageRows = this.aggregator.Aggregate(rowsByPlace);

        var kyiv = Assert.Single(coverageRows);
        Assert.Equal(3, kyiv.SkippedDays);
        Assert.Equal("2003-01-02..2003-01-04", kyiv.SkippedDates);
    }

    [Fact]
    public void Aggregate_MultipleGaps_ClustersEachRun()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] =
            [
                CreateRow(new DateTime(2003, 1, 1)),
                CreateRow(new DateTime(2003, 1, 3)),
                CreateRow(new DateTime(2003, 1, 6)),
            ],
        };

        var coverageRows = this.aggregator.Aggregate(rowsByPlace);

        var kyiv = Assert.Single(coverageRows);
        Assert.Equal(3, kyiv.SkippedDays);
        Assert.Equal("2003-01-02,2003-01-04..2003-01-05", kyiv.SkippedDates);
    }

    [Fact]
    public void Aggregate_MultipleRowsSameDay_CountsDateOnce()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] =
            [
                CreateRow(new DateTime(2003, 1, 1, 0, 0, 0)),
                CreateRow(new DateTime(2003, 1, 1, 6, 0, 0)),
                CreateRow(new DateTime(2003, 1, 2, 0, 0, 0)),
            ],
        };

        var coverageRows = this.aggregator.Aggregate(rowsByPlace);

        var kyiv = Assert.Single(coverageRows);
        Assert.Equal("2003-01-01", kyiv.FirstDate);
        Assert.Equal("2003-01-02", kyiv.LastDate);
        Assert.Equal(0, kyiv.SkippedDays);
        Assert.Equal(string.Empty, kyiv.SkippedDates);
    }

    [Fact]
    public void Aggregate_EmptyPlace_ReturnsNullDatesAndZeroSkippedDays()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] = [],
        };

        var coverageRows = this.aggregator.Aggregate(rowsByPlace);

        var kyiv = Assert.Single(coverageRows);
        Assert.Equal("Kyiv", kyiv.Place);
        Assert.Null(kyiv.FirstDate);
        Assert.Null(kyiv.LastDate);
        Assert.Equal(0, kyiv.SkippedDays);
        Assert.Equal(string.Empty, kyiv.SkippedDates);
    }

    [Fact]
    public void Aggregate_OrdersPlacesAlphabetically()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] = [CreateRow(new DateTime(2003, 1, 1))],
            ["Kharkiv"] = [CreateRow(new DateTime(2003, 1, 1))],
        };

        var coverageRows = this.aggregator.Aggregate(rowsByPlace);

        Assert.Equal(["Kharkiv", "Kyiv"], coverageRows.Select(row => row.Place));
    }

    private static WeatherDataRow CreateRow(DateTime time) =>
        new(time, WeatherCharacteristics.Clear, -5, 0, 1.0m, 750, 70);
}
