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

public sealed class PlaceConverterPreWarmTests
{
    private readonly PlaceConverter placeConverter = new();

    [Fact]
    public void PreWarm_AllPlaceTranslationsResolveForEveryEnumValue()
    {
        foreach (Place place in Enum.GetValues<Place>())
        {
            var nameInHtml = this.placeConverter.ToNameInHtml(place);
            var displayName = this.placeConverter.ToDisplayName(place);
            var formatterDisplayName = EnumDisplayNameFormatter.ToDisplayName(place);

            Assert.False(string.IsNullOrWhiteSpace(nameInHtml));
            Assert.False(string.IsNullOrWhiteSpace(displayName));
            Assert.False(string.IsNullOrWhiteSpace(formatterDisplayName));
            Assert.Equal(displayName, formatterDisplayName);
            Assert.Equal(place, this.placeConverter.FromNameInHtml(nameInHtml));
        }
    }

    [Fact]
    public void PreWarm_GetAllPairs_IsComplete()
    {
        var pairs = this.placeConverter.GetAllPairs();
        var placesFromPairs = pairs.Select(pair => pair.Place).ToHashSet();

        Assert.Equal(Enum.GetValues<Place>().Length, pairs.Count);
        Assert.Equal(Enum.GetValues<Place>().Length, placesFromPairs.Count);

        foreach (Place place in Enum.GetValues<Place>())
        {
            Assert.Contains(place, placesFromPairs);
        }
    }

    [Fact]
    public void PreWarm_NameInHtmlValuesAreUnique()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (nameInHtml, place) in this.placeConverter.GetAllPairs())
        {
            Assert.True(
                seen.Add(nameInHtml),
                $"Duplicate NameInHtml '{nameInHtml}' for Place.{place}.");
        }

        Assert.Equal(Enum.GetValues<Place>().Length, seen.Count);
    }

    [Fact]
    public void PreWarm_DisplayNamesAreUnique()
    {
        var seen = new Dictionary<string, Place>(StringComparer.OrdinalIgnoreCase);

        foreach (Place place in Enum.GetValues<Place>())
        {
            var displayName = this.placeConverter.ToDisplayName(place);

            if (!seen.TryAdd(displayName, place))
            {
                Assert.Fail(
                    $"Duplicate English display name '{displayName}' for Place.{place} and Place.{seen[displayName]}.");
            }
        }

        Assert.Equal(Enum.GetValues<Place>().Length, seen.Count);
    }

    [Theory]
    [InlineData("Chervonaya-Zirka", Place.ChervonaZirka)]
    [InlineData("Donetsk", Place.Donetsk)]
    [InlineData("Goverla", Place.Hoverla)]
    [InlineData("Gremyach", Place.Hremiach)]
    [InlineData("Hremyach", Place.Hremiach)]
    [InlineData("Harkov", Place.Kharkiv)]
    [InlineData("Ivano-Frankovsk", Place.IvanoFrankivsk)]
    [InlineData("Jitomir", Place.Zhytomyr)]
    [InlineData("Kiev", Place.Kyiv)]
    [InlineData("Kuyalnik", Place.Kuyalnyk)]
    [InlineData("Lugansk", Place.Luhansk)]
    [InlineData("Lvov", Place.Lviv)]
    [InlineData("Mariupol", Place.Mariupol)]
    [InlineData("Odessa", Place.Odesa)]
    [InlineData("Sevastopol", Place.Sevastopol)]
    [InlineData("Simferopol", Place.Simferopol)]
    [InlineData("Slavyansk", Place.Sloviansk)]
    [InlineData("Solomonovo", Place.Solomonove)]
    [InlineData("Ternopol", Place.Ternopil)]
    [InlineData("Ujgorod", Place.Uzhhorod)]
    public void PreWarm_KnownPathAliasesResolveConsistently(string folderName, Place expected)
    {
        var filePath = $"/data/weather/Real/{folderName}/2003-01-01.html";

        Assert.True(this.placeConverter.TryFromFilePath(filePath, out var place));
        Assert.Equal(expected, place);
    }
}
