// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Execution;

using Xunit;

public sealed class DurationFormatterTests
{
    [Theory]
    [InlineData(0, "00:00:00.000")]
    [InlineData(0.5, "00:00:00.500")]
    [InlineData(6.965, "00:00:06.965")]
    [InlineData(4522.15, "01:15:22.150")]
    [InlineData(9045.123, "02:30:45.123")]
    public void FormatSeconds_FormatsHoursMinutesSecondsAndMilliseconds(double totalSeconds, string expected)
    {
        Assert.Equal(expected, DurationFormatter.FormatSeconds(totalSeconds));
    }

    [Fact]
    public void Format_TimeSpanOverload_FormatsSameAsFormatSeconds()
    {
        var duration = TimeSpan.FromHours(1) + TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(22.15);

        Assert.Equal("01:15:22.150", DurationFormatter.Format(duration));
        Assert.Equal(DurationFormatter.Format(duration), DurationFormatter.FormatSeconds(duration.TotalSeconds));
    }

    [Fact]
    public void Format_DoesNotThrow_ForTypicalReportDurations()
    {
        var exception = Record.Exception(() =>
        {
            DurationFormatter.FormatSeconds(123.456);
            DurationFormatter.FormatSeconds(86_400);
            DurationFormatter.Format(TimeSpan.FromMilliseconds(1));
        });

        Assert.Null(exception);
    }
}
