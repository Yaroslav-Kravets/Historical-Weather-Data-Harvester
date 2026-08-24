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
    public void Format_SingleDay_ReturnsCompactDate()
    {
        var result = this.formatter.Format([new DateTime(2003, 1, 2)]);

        Assert.Equal("2003-01-02", result);
    }

    [Fact]
    public void Format_TwoConsecutiveDays_ReturnsAbbreviatedRange()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2003, 1, 2),
            new DateTime(2003, 1, 3),
        ]);

        Assert.Equal("2003-01-02..03", result);
    }

    [Fact]
    public void Format_ThreeOrMoreConsecutiveDays_ReturnsAbbreviatedRange()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2025, 5, 1),
            new DateTime(2025, 5, 2),
            new DateTime(2025, 5, 3),
        ]);

        Assert.Equal("2025-05-01..03", result);
    }

    [Fact]
    public void Format_MultipleClusters_JoinsWithCommaAndSpaceAndSameMonthChaining()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2003, 1, 2),
            new DateTime(2003, 1, 4),
            new DateTime(2003, 1, 5),
        ]);

        Assert.Equal("2003-01-02, 04..05", result);
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

        Assert.Equal("2003-01-02, 04..05", result);
    }

    [Fact]
    public void Format_TimeOfDayIgnored()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2003, 1, 1, 23, 59, 0),
            new DateTime(2003, 1, 2, 0, 0, 0),
        ]);

        Assert.Equal("2003-01-01..02", result);
    }

    [Fact]
    public void Format_CrossYearRange_UsesCompactFullEndDate()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2003, 1, 2),
            new DateTime(2004, 1, 2),
        ]);

        Assert.Equal("2003-01-02; 2004-01-02", result);
    }

    [Fact]
    public void Format_SameYearDifferentMonthRange_AbbreviatesMonthDayEnd()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2003, 1, 2),
            new DateTime(2003, 1, 3),
            new DateTime(2003, 3, 5),
        ]);

        Assert.Equal("2003-01-02..03, 03-05", result);
    }

    [Fact]
    public void FormatRanges_SameYearMultipleClusters_ChainsWithCommaAndAbbreviatesYearPrefix()
    {
        var result = this.formatter.FormatRanges(
        [
            (new DateTime(2011, 10, 30), new DateTime(2011, 10, 30)),
            (new DateTime(2011, 11, 22), new DateTime(2011, 12, 13)),
            (new DateTime(2011, 12, 31), new DateTime(2011, 12, 31)),
        ]);

        Assert.Equal("2011-10-30, 11-22..12-13, 12-31", result);
    }

    [Fact]
    public void Format_CrossYearWithSameMonthGapsInLastYear_UsesSemicolonBetweenYears()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2017, 10, 29),
            new DateTime(2018, 9, 19),
            new DateTime(2018, 9, 23),
        ]);

        Assert.Equal("2017-10-29; 2018-09-19, 23", result);
    }

    [Fact]
    public void FormatRanges_MultiYearRealWorldPattern_CombinesAllSeparatorAndAbbreviationRules()
    {
        var result = this.formatter.FormatRanges(
        [
            (new DateTime(2011, 10, 30), new DateTime(2011, 10, 30)),
            (new DateTime(2011, 11, 22), new DateTime(2011, 12, 13)),
            (new DateTime(2011, 12, 31), new DateTime(2011, 12, 31)),
            (new DateTime(2012, 10, 28), new DateTime(2012, 10, 28)),
            (new DateTime(2013, 10, 27), new DateTime(2013, 10, 27)),
            (new DateTime(2014, 10, 26), new DateTime(2014, 10, 26)),
            (new DateTime(2015, 1, 31), new DateTime(2015, 2, 2)),
            (new DateTime(2015, 10, 25), new DateTime(2015, 10, 25)),
            (new DateTime(2016, 10, 30), new DateTime(2016, 10, 30)),
            (new DateTime(2017, 10, 29), new DateTime(2017, 10, 29)),
            (new DateTime(2018, 9, 19), new DateTime(2018, 9, 19)),
            (new DateTime(2018, 9, 23), new DateTime(2018, 9, 23)),
        ]);

        Assert.Equal(
            "2011-10-30, 11-22..12-13, 12-31; 2012-10-28; 2013-10-27; 2014-10-26; "
            + "2015-01-31..02-02, 10-25; 2016-10-30; 2017-10-29; 2018-09-19, 23",
            result);
    }

    [Fact]
    public void Format_MixedIndividualDaysAcrossYears_NormalizesAndAppliesAllRules()
    {
        var result = this.formatter.Format(
        [
            new DateTime(2015, 10, 25),
            new DateTime(2015, 1, 31),
            new DateTime(2015, 2, 1),
            new DateTime(2015, 2, 2),
            new DateTime(2014, 10, 26),
            new DateTime(2018, 9, 23),
            new DateTime(2018, 9, 19),
            new DateTime(2017, 10, 29),
            new DateTime(2011, 12, 31),
            new DateTime(2011, 10, 30),
        ]);

        Assert.Equal(
            "2011-10-30, 12-31; 2014-10-26; 2015-01-31..02-02, 10-25; "
            + "2017-10-29; 2018-09-19, 23",
            result);
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
    public void FormatRanges_SingleDayAndMultiDayRange_JoinsWithCommaAndSpaceAndSameMonthChaining()
    {
        var result = this.formatter.FormatRanges(
        [
            (new DateTime(2003, 1, 2), new DateTime(2003, 1, 2)),
            (new DateTime(2003, 1, 4), new DateTime(2003, 1, 5)),
        ]);

        Assert.Equal("2003-01-02, 04..05", result);
    }

    [Fact]
    public void FormatRanges_UnsortedInput_ProducesSameOutputAsSorted()
    {
        var expected = this.formatter.FormatRanges(
        [
            (new DateTime(2003, 1, 2), new DateTime(2003, 1, 2)),
            (new DateTime(2003, 1, 4), new DateTime(2003, 1, 5)),
        ]);

        var result = this.formatter.FormatRanges(
        [
            (new DateTime(2003, 1, 4), new DateTime(2003, 1, 5)),
            (new DateTime(2003, 1, 2), new DateTime(2003, 1, 2)),
        ]);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatRanges_CrossYearWideRange_UsesCompactFullEndDate()
    {
        var result = this.formatter.FormatRanges(
        [
            (new DateTime(2000, 1, 2), new DateTime(2019, 12, 31)),
        ]);

        Assert.Equal("2000-01-02..2019-12-31", result);
    }

    [Fact]
    public void FormatRanges_SameYearDifferentMonthRange_AbbreviatesMonthDayEnd()
    {
        var result = this.formatter.FormatRanges(
        [
            (new DateTime(2003, 1, 2), new DateTime(2003, 3, 5)),
        ]);

        Assert.Equal("2003-01-02..03-05", result);
    }
}
