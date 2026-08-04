// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Enums;

using Xunit;

public sealed class PlaceTestDataTests
{
    private readonly PlaceConverter placeConverter = new();

    [Fact]
    public void AllRows_CoversEveryPlaceEnumValue()
    {
        TheoryDataCoverageAssertions.AssertCoversAllEnumValues(
            PlaceTestData.AllRows.Select(row => row.Place),
            nameof(PlaceTestData.AllRows));
        TheoryDataCoverageAssertions.AssertRowCountMatchesEnumCount<Place>(
            PlaceTestData.AllRows.Length,
            nameof(PlaceTestData.AllRows));
    }

    [Fact]
    public void AllRows_NameInHtml_IsUnique()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (place, nameInHtml, _) in PlaceTestData.AllRows)
        {
            Assert.True(
                seen.Add(nameInHtml),
                $"Duplicate NameInHtml '{nameInHtml}' for Place.{place}.");
        }

        Assert.Equal(PlaceTestData.AllRows.Length, seen.Count);
    }

    [Fact]
    public void AllRows_DisplayName_IsUnique()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (place, _, displayName) in PlaceTestData.AllRows)
        {
            Assert.True(
                seen.Add(displayName),
                $"Duplicate display name '{displayName}' for Place.{place}.");
        }

        Assert.Equal(PlaceTestData.AllRows.Length, seen.Count);
    }

    [Fact]
    public void AllRows_NameInHtmlMatchesPlaceConverter()
    {
        foreach (var (place, expectedNameInHtml, _) in PlaceTestData.AllRows)
        {
            Assert.Equal(expectedNameInHtml, this.placeConverter.ToNameInHtml(place));
        }
    }

    [Fact]
    public void AllRows_DisplayNameMatchesEnumDisplayNameFormatter()
    {
        foreach (var (place, _, expectedDisplayName) in PlaceTestData.AllRows)
        {
            Assert.Equal(expectedDisplayName, EnumDisplayNameFormatter.ToDisplayName(place));
        }
    }

    [Fact]
    public void AllRows_DisplayNameMatchesPlaceConverter()
    {
        foreach (var (place, _, expectedDisplayName) in PlaceTestData.AllRows)
        {
            Assert.Equal(expectedDisplayName, this.placeConverter.ToDisplayName(place));
        }
    }
}
