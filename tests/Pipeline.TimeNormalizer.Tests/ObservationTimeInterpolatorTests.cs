// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class ObservationTimeInterpolatorTests
{
    private static readonly IReadOnlyList<TimeSpan> ExpectedTimes = Enumerable.Range(0, 8)
        .Select(i => TimeSpan.FromHours(i * 3))
        .ToList();

    private readonly ObservationTimeInterpolator interpolator =
        new(NullLogger<ObservationTimeInterpolator>.Instance);

    [Fact]
    public void TryInterpolateMissingObservationTimes_FillsMissingTimesFromNeighbors()
    {
        var date = new DateTime(2003, 1, 2);
        var previousDate = date.AddDays(-1);
        var nextDate = date.AddDays(1);

        var dayRows = ExpectedTimes
            .Where(time => time != TimeSpan.FromHours(6))
            .Select(time => new WeatherDataRow(
                date.Add(time),
                WeatherCharacteristics.Clear,
                0,
                180,
                2.0m,
                750,
                60))
            .ToList();

        var previousDayRows = ExpectedTimes
            .Select(time => new WeatherDataRow(
                previousDate.Add(time),
                WeatherCharacteristics.Clear,
                -2,
                180,
                2.0m,
                750,
                60))
            .ToList();

        var nextDayRows = ExpectedTimes
            .Select(time => new WeatherDataRow(
                nextDate.Add(time),
                WeatherCharacteristics.Clear,
                2,
                180,
                2.0m,
                750,
                60))
            .ToList();

        var success = this.interpolator.TryInterpolateMissingObservationTimes(
            "/path/file.html",
            date,
            dayRows,
            ExpectedTimes,
            previousDayRows,
            nextDayRows,
            out var interpolatedRows);

        Assert.True(success);
        Assert.Equal(8, interpolatedRows.Count);
        Assert.Contains(interpolatedRows, row => row.Time.TimeOfDay == TimeSpan.FromHours(6));
    }

    [Fact]
    public void TryInterpolateMissingObservationTimes_ReturnsFalseWhenNotEnoughCandidates()
    {
        var date = new DateTime(2003, 1, 2);
        var dayRows = new List<WeatherDataRow>
        {
            new(date.AddHours(12), WeatherCharacteristics.Clear, 0, 180, 2.0m, 750, 60),
        };

        var success = this.interpolator.TryInterpolateMissingObservationTimes(
            "/path/file.html",
            date,
            dayRows,
            ExpectedTimes,
            null,
            null,
            out _);

        Assert.False(success);
    }
}
