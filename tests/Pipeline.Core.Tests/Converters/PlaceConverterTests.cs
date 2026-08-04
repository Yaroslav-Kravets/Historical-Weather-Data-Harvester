// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Converters;

using Xunit;

public sealed class PlaceConverterTests
{
    private readonly PlaceConverter placeConverter = new();

    public static TheoryData<string, Place> AllNameInHtmlToPlacePairs { get; } = CreateAllNameInHtmlToPlacePairs();

    public static TheoryData<Place, string> AllPlaceToNameInHtmlPairs { get; } = CreateAllPlaceToNameInHtmlPairs();

    public static TheoryData<Place, string> AllPlaceToDisplayNamePairs { get; } = CreateAllPlaceToDisplayNamePairs();

    [Fact]
    public void AllPlaceToDisplayNamePairs_CoversEveryPlaceEnumValue()
    {
        TheoryDataCoverageAssertions.AssertCoversAllEnumValues(
            PlaceTestData.AllRows.Select(row => row.Place),
            nameof(AllPlaceToDisplayNamePairs));
        TheoryDataCoverageAssertions.AssertRowCountMatchesEnumCount<Place>(
            PlaceTestData.AllRows.Length,
            nameof(AllPlaceToDisplayNamePairs));
    }

    [Fact]
    public void AllNameInHtmlToPlacePairs_CoversEveryPlaceEnumValue()
    {
        TheoryDataCoverageAssertions.AssertCoversAllEnumValues(
            PlaceTestData.AllRows.Select(row => row.Place),
            nameof(AllNameInHtmlToPlacePairs));
        TheoryDataCoverageAssertions.AssertRowCountMatchesEnumCount<Place>(
            PlaceTestData.AllRows.Length,
            nameof(AllNameInHtmlToPlacePairs));
    }

    [Theory]
    [MemberData(nameof(AllNameInHtmlToPlacePairs))]
    public void FromNameInHtml_ReturnsPlace_ForEveryConfiguredName(string nameInHtml, Place expected)
    {
        Assert.Equal(expected, this.placeConverter.FromNameInHtml(nameInHtml));
    }

    [Fact]
    public void FromNameInHtml_IsCaseInsensitive()
    {
        Assert.Equal(Place.Kyiv, this.placeConverter.FromNameInHtml("КИЕВЕ"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromNameInHtml_ThrowsArgumentException_ForNullOrWhitespace(string? nameInHtml)
    {
        Assert.Throws<ArgumentException>(() => this.placeConverter.FromNameInHtml(nameInHtml!));
    }

    [Fact]
    public void FromNameInHtml_Throws_ForUnknownPlace()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            this.placeConverter.FromNameInHtml("Неизвестное Место"));
        Assert.Contains("Unknown place", ex.Message);
    }

    [Fact]
    public void FromNameInHtml_Throws_WithContext_WhenContextProvided()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            this.placeConverter.FromNameInHtml("Неизвестное Место", "/path/to/file.html"));
        Assert.Contains("in /path/to/file.html", ex.Message);
    }

    [Fact]
    public void TryFromNameInHtml_ReturnsFalse_ForUnknownPlace()
    {
        Assert.False(this.placeConverter.TryFromNameInHtml("Неизвестное Место", out var place));
        Assert.Equal(default, place);
    }

    [Fact]
    public void TryFromNameInHtml_ReturnsTrue_ForKnownPlace()
    {
        Assert.True(this.placeConverter.TryFromNameInHtml("Киеве", out var place));
        Assert.Equal(Place.Kyiv, place);
    }

    [Theory]
    [MemberData(nameof(AllPlaceToDisplayNamePairs))]
    public void ToDisplayName_ResolvesDisplayName_ForEveryPlace(Place place, string expectedDisplayName)
    {
        Assert.Equal(expectedDisplayName, this.placeConverter.ToDisplayName(place));
    }

    [Fact]
    public void EveryPlace_HasResolvableDisplayName()
    {
        foreach (Place place in Enum.GetValues<Place>())
        {
            var displayName = this.placeConverter.ToDisplayName(place);
            Assert.False(string.IsNullOrWhiteSpace(displayName));
        }
    }

    [Theory]
    [MemberData(nameof(AllPlaceToNameInHtmlPairs))]
    public void ToNameInHtml_ReturnsConfiguredName_ForEveryPlace(Place place, string expectedNameInHtml)
    {
        Assert.Equal(expectedNameInHtml, this.placeConverter.ToNameInHtml(place));
    }

    [Theory]
    [InlineData("/data/weather/Real/Kiev/2003-1-1.html", Place.Kyiv)]
    [InlineData("/data/weather/Real/Jitomir/2003/1.html", Place.Zhytomyr)]
    [InlineData("/data/weather/Real/Chervonaya-Zirka/file.html", Place.ChervonaZirka)]
    public void TryFromFilePath_ResolvesPlaceFromPathSegment(string filePath, Place expected)
    {
        Assert.True(this.placeConverter.TryFromFilePath(filePath, out var place));
        Assert.Equal(expected, place);
    }

    [Fact]
    public void TryFromFilePath_ReturnsFalse_WhenNoPlaceSegmentFound()
    {
        Assert.False(this.placeConverter.TryFromFilePath("/tmp/no-place-here/file.html", out _));
    }

    [Theory]
    [InlineData("/data/weather/Real/Kiev.html")]
    [InlineData("/data/weather/Real/misc/Kiev.html")]
    [InlineData("/tmp/Kiev")]
    public void TryFromFilePath_ReturnsFalse_WhenPlaceNameAppearsOnlyInFileName(string filePath)
    {
        Assert.False(this.placeConverter.TryFromFilePath(filePath, out _));
    }

    [Fact]
    public void TryFromFilePath_ResolvesPlaceFromDirectory_WhenFileNameAlsoContainsPlaceName()
    {
        Assert.True(this.placeConverter.TryFromFilePath("/data/weather/Real/Kiev/Kiev.html", out var place));
        Assert.Equal(Place.Kyiv, place);
    }

    [Fact]
    public void ConverterTable_ContainsEntryForEveryPlaceEnumValue()
    {
        var placesFromConverter = this.placeConverter.GetAllPairs()
            .Select(pair => pair.Place)
            .ToHashSet();

        Assert.Equal(Enum.GetValues<Place>().Length, placesFromConverter.Count);

        foreach (Place place in Enum.GetValues<Place>())
        {
            Assert.True(
                placesFromConverter.Contains(place),
                $"Place.{place} has no entry in PlaceConverter.");
        }
    }

    [Fact]
    public void ConverterTable_EveryPlaceRoundTripsNameInHtmlToDisplayName()
    {
        foreach (var (place, nameInHtml, displayName) in PlaceTestData.AllRows)
        {
            Assert.Equal(displayName, this.placeConverter.ToDisplayNameFromNameInHtml(nameInHtml));
            Assert.Equal(nameInHtml, this.placeConverter.ToNameInHtml(place));
        }
    }

    private static TheoryData<string, Place> CreateAllNameInHtmlToPlacePairs()
    {
        var data = new TheoryData<string, Place>();

        foreach (var (place, nameInHtml, _) in PlaceTestData.AllRows)
        {
            data.Add(nameInHtml, place);
        }

        return data;
    }

    private static TheoryData<Place, string> CreateAllPlaceToNameInHtmlPairs()
    {
        var data = new TheoryData<Place, string>();

        foreach (var (place, nameInHtml, _) in PlaceTestData.AllRows)
        {
            data.Add(place, nameInHtml);
        }

        return data;
    }

    private static TheoryData<Place, string> CreateAllPlaceToDisplayNamePairs()
    {
        var data = new TheoryData<Place, string>();

        foreach (var (place, _, displayName) in PlaceTestData.AllRows)
        {
            data.Add(place, displayName);
        }

        return data;
    }
}
