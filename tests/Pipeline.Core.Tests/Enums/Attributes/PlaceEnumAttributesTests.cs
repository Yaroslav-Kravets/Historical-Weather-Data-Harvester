// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Enums.Attributes;

using System.Reflection;
using Xunit;

public sealed class PlaceEnumAttributesTests
{
    [Fact]
    public void EveryPlace_HasNameInHtmlAttribute()
    {
        foreach (Place place in Enum.GetValues<Place>())
        {
            var field = typeof(Place).GetField(place.ToString(), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);

            var attribute = field.GetCustomAttribute<NameInHtmlAttribute>();
            Assert.NotNull(attribute);
            Assert.False(string.IsNullOrWhiteSpace(attribute.NameInHtml));
        }
    }

    [Fact]
    public void OnlyPlacesWithDistinctDisplayName_HaveEnumDisplayNameAttribute()
    {
        var placesWithAttribute = new HashSet<Place>();

        foreach (Place place in Enum.GetValues<Place>())
        {
            var field = typeof(Place).GetField(place.ToString(), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);

            var attribute = field.GetCustomAttribute<EnumDisplayNameAttribute>();
            if (attribute is not null)
            {
                Assert.False(string.IsNullOrWhiteSpace(attribute.Name));
                placesWithAttribute.Add(place);
            }
        }

        Assert.Equal(
            new[] { Place.ChervonaZirka, Place.IvanoFrankivsk },
            placesWithAttribute.OrderBy(place => place.ToString(), StringComparer.Ordinal));
    }

    [Fact]
    public void AllRows_NameInHtmlMatchesPlaceNameInHtmlAttribute()
    {
        foreach (var (place, expectedNameInHtml, _) in PlaceTestData.AllRows)
        {
            var field = typeof(Place).GetField(place.ToString(), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);

            var attribute = field.GetCustomAttribute<NameInHtmlAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal(expectedNameInHtml, attribute.NameInHtml);
        }
    }
}
