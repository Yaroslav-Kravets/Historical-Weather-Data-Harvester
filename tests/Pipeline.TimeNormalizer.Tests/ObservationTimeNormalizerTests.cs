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

public sealed class ObservationTimeNormalizerTests
{
    private static readonly IReadOnlyList<TimeSpan> ExpectedTimes = Enumerable.Range(0, 8)
        .Select(i => TimeSpan.FromHours(i * 3))
        .ToList();

    private readonly ObservationTimeNormalizer normalizer =
        new(NullLogger<ObservationTimeNormalizer>.Instance);

    [Fact]
    public void NormalizeOrThrow_ReturnsEightRowsForCompleteDay()
    {
        var date = new DateTime(2003, 1, 1);
        var dayRows = ExpectedTimes
            .Select(time => new WeatherDataRow(
                date.Add(time),
                WeatherCharacteristics.Clear,
                -5,
                180,
                2.0m,
                750,
                60))
            .ToList();

        var missingCount = 0;
        var normalized = this.normalizer.NormalizeOrThrow(
            "/path/file.html",
            dayRows,
            ExpectedTimes,
            ref missingCount);

        Assert.Equal(8, normalized.Count);
        Assert.Equal(0, missingCount);
    }

    [Fact]
    public void NormalizeOrThrow_ThrowsWhenObservationTimeMissing()
    {
        var date = new DateTime(2003, 1, 1);
        var dayRows = new List<WeatherDataRow>
        {
            new(date, WeatherCharacteristics.Clear, -5, 180, 2.0m, 750, 60),
        };

        var missingCount = 0;

        Assert.Throws<InvalidOperationException>(() =>
            this.normalizer.NormalizeOrThrow(
                "/path/file.html",
                dayRows,
                ExpectedTimes,
                ref missingCount));

        Assert.Equal(1, missingCount);
    }

    [Fact]
    public void NormalizeOrThrow_ThrowsArgumentException_WhenExpectedObservationTimesIsEmpty()
    {
        var missingCount = 0;

        Assert.Throws<ArgumentException>(() =>
            this.normalizer.NormalizeOrThrow(
                "/path/file.html",
                [],
                [],
                ref missingCount));
    }
}
