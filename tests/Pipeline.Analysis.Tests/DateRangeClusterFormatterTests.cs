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
}
