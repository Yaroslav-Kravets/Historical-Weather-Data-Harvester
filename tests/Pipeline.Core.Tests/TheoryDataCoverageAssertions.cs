// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests;

using Xunit;

internal static class TheoryDataCoverageAssertions
{
    public static void AssertCoversAllEnumValues<TEnum>(IEnumerable<TEnum> actual, string tableName)
        where TEnum : struct, Enum
    {
        var expected = Enum.GetValues<TEnum>().ToHashSet();
        var actualSet = actual.ToHashSet();

        Assert.True(
            expected.SetEquals(actualSet),
            $"{tableName} must contain exactly one row per {typeof(TEnum).Name} enum value.");
        Assert.Equal(expected.Count, actualSet.Count);
    }

    public static void AssertRowCountMatchesEnumCount<TEnum>(int rowCount, string tableName)
        where TEnum : struct, Enum
    {
        var expectedCount = Enum.GetValues<TEnum>().Length;
        Assert.True(
            rowCount == expectedCount,
            $"{tableName} row count must match {typeof(TEnum).Name} enum value count. Expected {expectedCount}, got {rowCount}.");
    }

    public static void AssertCoversAllEnumValuesExcept<TEnum>(
        IEnumerable<TEnum> actual,
        TEnum excluded,
        string tableName)
        where TEnum : struct, Enum
    {
        var expected = Enum.GetValues<TEnum>()
            .Where(value => !value.Equals(excluded))
            .ToHashSet();
        var actualSet = actual.ToHashSet();

        Assert.True(
            expected.SetEquals(actualSet),
            $"{tableName} must contain exactly one row per {typeof(TEnum).Name} enum value except {excluded}.");
        Assert.Equal(expected.Count, actualSet.Count);
    }

    public static void AssertRowCountMatchesEnumCountExcept<TEnum>(int rowCount, TEnum excluded, string tableName)
        where TEnum : struct, Enum
    {
        var expectedCount = Enum.GetValues<TEnum>().Count(value => !value.Equals(excluded));

        Assert.True(
            rowCount == expectedCount,
            $"{tableName} row count must match {typeof(TEnum).Name} enum value count except {excluded}. Expected {expectedCount}, got {rowCount}.");
    }
}
