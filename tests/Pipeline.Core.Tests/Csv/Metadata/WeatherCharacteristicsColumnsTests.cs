// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv.Metadata;

using Xunit;

public sealed class WeatherCharacteristicsColumnsTests
{
    [Fact]
    public void All_ContainsEveryEnumValueExceptNone()
    {
        TheoryDataCoverageAssertions.AssertCoversAllEnumValuesExcept(
            WeatherCharacteristicsColumns.All.Select(pair => pair.Flag),
            WeatherCharacteristics.None,
            nameof(WeatherCharacteristicsColumns));
    }

    [Fact]
    public void All_ColumnNamesAreUniqueAndSortedAlphabetically()
    {
        var columnNames = WeatherCharacteristicsColumns.All.Select(pair => pair.ColumnName).ToList();

        Assert.Equal(columnNames.Count, columnNames.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(columnNames, columnNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList());
    }

    [Fact]
    public void All_FlagsAreUnique()
    {
        var flags = WeatherCharacteristicsColumns.All.Select(pair => pair.Flag).ToList();

        Assert.Equal(flags.Count, flags.Distinct().Count());
        TheoryDataCoverageAssertions.AssertRowCountMatchesEnumCountExcept(
            WeatherCharacteristicsColumns.All.Count,
            WeatherCharacteristics.None,
            nameof(WeatherCharacteristicsColumns));
    }
}
