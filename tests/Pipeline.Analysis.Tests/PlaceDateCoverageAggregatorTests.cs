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
    public void Aggregate_Throws_WhenRowsByPlaceNull()
    {
        Assert.Throws<ArgumentNullException>(() => this.aggregator.Aggregate(null!));
    }

    [Fact]
    public void Aggregate_Throws_WhenPlaceRowsNull()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] = null!,
        };

        Assert.Throws<ArgumentNullException>(() => this.aggregator.Aggregate(rowsByPlace));
    }

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
        Assert.Equal(3, kyiv.ObservedDays);
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
        Assert.Equal(2, kyiv.ObservedDays);
        Assert.Equal(1, kyiv.SkippedDays);
        Assert.Equal("2003-01-02", kyiv.SkippedDates);
    }

    [Fact]
    public void Aggregate_MultiYearGaps_UsesSemicolonBetweenYears()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] =
            [
                CreateRow(new DateTime(2011, 10, 30)),
                CreateRow(new DateTime(2011, 11, 1)),
                CreateRow(new DateTime(2018, 9, 19)),
                CreateRow(new DateTime(2018, 9, 23)),
            ],
        };

        var coverageRows = this.aggregator.Aggregate(rowsByPlace);

        var kyiv = Assert.Single(coverageRows);
        Assert.Equal("2011-10-30", kyiv.FirstDate);
        Assert.Equal("2018-09-23", kyiv.LastDate);
        Assert.Equal(4, kyiv.ObservedDays);
        Assert.Equal(
            (new DateTime(2018, 9, 23) - new DateTime(2011, 10, 30)).Days + 1 - 4,
            kyiv.SkippedDays);
        Assert.Equal("2011-10-31, 11-02..2018-09-18; 2018-09-20..22", kyiv.SkippedDates);
    }

    [Fact]
    public void Aggregate_CrossYearContiguousGap_KeepsSingleRangeCluster()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] =
            [
                CreateRow(new DateTime(2011, 12, 30)),
                CreateRow(new DateTime(2012, 1, 3)),
            ],
        };

        var coverageRows = this.aggregator.Aggregate(rowsByPlace);

        var kyiv = Assert.Single(coverageRows);
        Assert.Equal("2011-12-30", kyiv.FirstDate);
        Assert.Equal("2012-01-03", kyiv.LastDate);
        Assert.Equal(2, kyiv.ObservedDays);
        Assert.Equal(3, kyiv.SkippedDays);
        Assert.Equal("2011-12-31..2012-01-02", kyiv.SkippedDates);
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
        Assert.Equal(2, kyiv.ObservedDays);
        Assert.Equal(3, kyiv.SkippedDays);
        Assert.Equal("2003-01-02..04", kyiv.SkippedDates);
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
        Assert.Equal(3, kyiv.ObservedDays);
        Assert.Equal(3, kyiv.SkippedDays);
        Assert.Equal("2003-01-02, 04..05", kyiv.SkippedDates);
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
        Assert.Equal(2, kyiv.ObservedDays);
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
        Assert.Equal(0, kyiv.ObservedDays);
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

    [Fact]
    public void Aggregate_WideSpanWithSparseObservations_ComputesSkippedDaysWithoutEnumeratingEachDay()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] =
            [
                CreateRow(new DateTime(2000, 1, 1)),
                CreateRow(new DateTime(2020, 1, 1)),
            ],
        };

        var coverageRows = this.aggregator.Aggregate(rowsByPlace);

        var kyiv = Assert.Single(coverageRows);
        Assert.Equal("2000-01-01", kyiv.FirstDate);
        Assert.Equal("2020-01-01", kyiv.LastDate);
        Assert.Equal(2, kyiv.ObservedDays);
        Assert.Equal(7304, kyiv.SkippedDays);
        Assert.Equal("2000-01-02..2019-12-31", kyiv.SkippedDates);
    }

    private static WeatherDataRow CreateRow(DateTime time) =>
        new(time, WeatherCharacteristics.Clear, -5, 0, 1.0m, 750, 70);
}
