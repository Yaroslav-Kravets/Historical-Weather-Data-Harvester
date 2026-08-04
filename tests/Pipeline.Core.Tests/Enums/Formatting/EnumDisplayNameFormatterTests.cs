// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Enums.Formatting;

using Xunit;

public sealed class EnumDisplayNameFormatterTests
{
    private static readonly WeatherCharacteristicConverter WeatherCharacteristicConverter = new();

    public static TheoryData<Place, string> AllPlaceDisplayNames { get; } = CreateAllPlaceDisplayNames();

    public static TheoryData<WeatherCharacteristics, string> AllWeatherDisplayNames { get; } =
        CreateAllWeatherDisplayNames();

    [Fact]
    public void AllPlaceDisplayNames_CoversEveryPlaceEnumValue()
    {
        TheoryDataCoverageAssertions.AssertCoversAllEnumValues(
            PlaceTestData.AllRows.Select(row => row.Place),
            nameof(AllPlaceDisplayNames));
        TheoryDataCoverageAssertions.AssertRowCountMatchesEnumCount<Place>(
            PlaceTestData.AllRows.Length,
            nameof(AllPlaceDisplayNames));
    }

    [Fact]
    public void AllWeatherDisplayNames_CoversEveryDefinedWeatherCharacteristic()
    {
        TheoryDataCoverageAssertions.AssertCoversAllEnumValuesExcept(
            WeatherCharacteristicConverter.GetAllPairs().Select(pair => pair.Flag),
            WeatherCharacteristics.None,
            nameof(AllWeatherDisplayNames));
    }

    [Fact]
    public void AllWeatherDisplayNames_RowCountMatchesConverterTable()
    {
        TheoryDataCoverageAssertions.AssertRowCountMatchesEnumCountExcept(
            WeatherCharacteristicConverter.GetAllPairs().Count,
            WeatherCharacteristics.None,
            nameof(AllWeatherDisplayNames));
    }

    [Theory]
    [InlineData(Place.ChervonaZirka, "Chervona Zirka")]
    [InlineData(Place.IvanoFrankivsk, "Ivano-Frankivsk")]
    public void ToDisplayName_UsesPlaceNameAttribute(Place place, string expected)
    {
        Assert.Equal(expected, EnumDisplayNameFormatter.ToDisplayName(place));
    }

    [Fact]
    public void ToDisplayName_UsesPlaceNameAttribute_NotPascalCaseFallback_ForIvanoFrankivsk()
    {
        var displayName = EnumDisplayNameFormatter.ToDisplayName(Place.IvanoFrankivsk);

        Assert.Equal("Ivano-Frankivsk", displayName);
        Assert.NotEqual(PascalCaseNameFormatter.ToDisplayName(nameof(Place.IvanoFrankivsk)), displayName);
    }

    [Theory]
    [MemberData(nameof(AllPlaceDisplayNames))]
    public void ToDisplayName_ReturnsExpectedDisplayName_ForEveryPlace(Place place, string expectedEnglish)
    {
        Assert.Equal(expectedEnglish, EnumDisplayNameFormatter.ToDisplayName(place));
    }

    [Theory]
    [MemberData(nameof(AllWeatherDisplayNames))]
    public void ToDisplayName_MatchesPascalCaseFallback(
        WeatherCharacteristics flag,
        string expectedEnglish)
    {
        Assert.Equal(expectedEnglish, EnumDisplayNameFormatter.ToDisplayName(flag));
    }

    private static TheoryData<Place, string> CreateAllPlaceDisplayNames()
    {
        var data = new TheoryData<Place, string>();

        foreach (var (place, _, displayName) in PlaceTestData.AllRows)
        {
            data.Add(place, displayName);
        }

        return data;
    }

    private static TheoryData<WeatherCharacteristics, string> CreateAllWeatherDisplayNames()
    {
        var data = new TheoryData<WeatherCharacteristics, string>();

        foreach (var (_, flag) in WeatherCharacteristicConverter.GetAllPairs())
        {
            data.Add(flag, EnumDisplayNameFormatter.ToDisplayName(flag));
        }

        return data;
    }
}
