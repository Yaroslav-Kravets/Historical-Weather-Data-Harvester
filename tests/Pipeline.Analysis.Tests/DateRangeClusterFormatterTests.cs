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

public sealed class DateRangeClusterFormatterTests
{
    private readonly DateRangeClusterFormatter formatter = new();

    public static TheoryData<DateTime, string> FormatDateCases() => new()
    {
        { new DateTime(2003, 1, 2), "2003-01-02" },
        { new DateTime(2003, 1, 2, 15, 30, 0), "2003-01-02" },
        { new DateTime(2003, 1, 2, 23, 59, 59), "2003-01-02" },
        { new DateTime(2024, 2, 29), "2024-02-29" },
        { new DateTime(1999, 12, 31), "1999-12-31" },
        { new DateTime(2000, 1, 1, 6, 0, 0), "2000-01-01" },
        { new DateTime(2025, 5, 3, 12, 0, 0), "2025-05-03" },
    };

    [Fact]
    public void Format_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => this.formatter.Format(null!));
    }

    [Fact]
    public void Format_Empty_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, this.formatter.Format([]));
    }

    [Fact]
    public void Format_SingleDay_ReturnsSingleDate()
    {
        var result = this.formatter.Format([new DateTime(2003, 1, 2)]);

        Assert.Equal("2003-01-02", result);
    }

    [Fact]
    public void Format_TwoConsecutiveDays_ReturnsRange()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2003, 1, 2),
            new DateTime(2003, 1, 3),
        ]);

        Assert.Equal("2003-01-02..2003-01-03", result);
    }

    [Fact]
    public void Format_ThreeOrMoreConsecutiveDays_ReturnsSingleRange()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2025, 5, 1),
            new DateTime(2025, 5, 2),
            new DateTime(2025, 5, 3),
        ]);

        Assert.Equal("2025-05-01..2025-05-03", result);
    }

    [Fact]
    public void Format_MultipleClusters_JoinsWithComma()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2003, 1, 2),
            new DateTime(2003, 1, 4),
            new DateTime(2003, 1, 5),
        ]);

        Assert.Equal("2003-01-02,2003-01-04..2003-01-05", result);
    }

    [Fact]
    public void Format_UnsortedAndDuplicateInput_Normalizes()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2003, 1, 5),
            new DateTime(2003, 1, 2),
            new DateTime(2003, 1, 2, 12, 0, 0),
            new DateTime(2003, 1, 4),
        ]);

        Assert.Equal("2003-01-02,2003-01-04..2003-01-05", result);
    }

    [Fact]
    public void Format_TimeOfDayIgnored()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2003, 1, 1, 23, 59, 0),
            new DateTime(2003, 1, 2, 0, 0, 0),
        ]);

        Assert.Equal("2003-01-01..2003-01-02", result);
    }

    [Theory]
    [MemberData(nameof(FormatDateCases))]
    public void FormatDate_ReturnsYyyyMmDd(DateTime date, string expected)
    {
        Assert.Equal(expected, this.formatter.FormatDate(date));
    }

    [Fact]
    public void FormatRanges_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => this.formatter.FormatRanges(null!));
    }

    [Fact]
    public void FormatRanges_Empty_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, this.formatter.FormatRanges([]));
    }

    [Fact]
    public void FormatRanges_SingleDayAndMultiDayRange_JoinsWithComma()
    {
        var result = this.formatter.FormatRanges(
        [
            (new DateTime(2003, 1, 2), new DateTime(2003, 1, 2)),
            (new DateTime(2003, 1, 4), new DateTime(2003, 1, 5)),
        ]);

        Assert.Equal("2003-01-02,2003-01-04..2003-01-05", result);
    }
}
