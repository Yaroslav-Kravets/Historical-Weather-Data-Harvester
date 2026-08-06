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

public sealed class WeatherCharacteristicUsageAggregatorTests
{
    private readonly WeatherCharacteristicUsageAggregator aggregator =
        new(new WeatherCharacteristicConverter());

    [Fact]
    public void Aggregate_CountsRowsPerFlag_IncludingMultiFlagRows()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] =
            [
                CreateRow(WeatherCharacteristics.Clear),
                CreateRow(WeatherCharacteristics.Rain),
                CreateRow(WeatherCharacteristics.Clear | WeatherCharacteristics.Rain),
            ],
        };

        var usageRows = this.aggregator.Aggregate(rowsByPlace);

        var clear = usageRows.Single(row => row.EnglishName == "Clear");
        var rain = usageRows.Single(row => row.EnglishName == "Rain");
        var hail = usageRows.Single(row => row.EnglishName == "Hail");

        Assert.Equal(2, clear.RowCount);
        Assert.Equal(66.67, clear.PercentOfRows, 2);
        Assert.Equal(2, rain.RowCount);
        Assert.Equal(66.67, rain.PercentOfRows, 2);
        Assert.Equal(0, hail.RowCount);
        Assert.Equal(0.0, hail.PercentOfRows);
    }

    [Fact]
    public void Aggregate_WhenNoDataRows_ReturnsZeroCountsAndPercents()
    {
        var usageRows = this.aggregator.Aggregate(
            new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase));

        Assert.NotEmpty(usageRows);
        Assert.All(usageRows, row =>
        {
            Assert.Equal(0, row.RowCount);
            Assert.Equal(0.0, row.PercentOfRows);
        });
    }

    [Fact]
    public void Aggregate_OrdersByEnglishName()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] = [CreateRow(WeatherCharacteristics.Rain)],
        };

        var usageRows = this.aggregator.Aggregate(rowsByPlace);

        Assert.Equal(
            usageRows.Select(row => row.EnglishName).OrderBy(name => name, StringComparer.OrdinalIgnoreCase),
            usageRows.Select(row => row.EnglishName));
    }

    [Fact]
    public void Aggregate_NormalizesAcrossAllPlaceCsvs()
    {
        var rowsByPlace = new Dictionary<string, IReadOnlyList<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kyiv"] = [CreateRow(WeatherCharacteristics.Clear)],
            ["Kharkiv"] =
            [
                CreateRow(WeatherCharacteristics.Clear),
                CreateRow(WeatherCharacteristics.Rain),
            ],
        };

        var usageRows = this.aggregator.Aggregate(rowsByPlace);

        var clear = usageRows.Single(row => row.EnglishName == "Clear");
        var rain = usageRows.Single(row => row.EnglishName == "Rain");

        Assert.Equal(2, clear.RowCount);
        Assert.Equal(66.67, clear.PercentOfRows, 2);
        Assert.Equal(1, rain.RowCount);
        Assert.Equal(33.33, rain.PercentOfRows, 2);
    }

    private static WeatherDataRow CreateRow(WeatherCharacteristics characteristics) =>
        new(new DateTime(2003, 1, 1, 0, 0, 0), characteristics, -5, 0, 1.0m, 750, 70);
}
