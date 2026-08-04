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

public sealed class WindDirectionAzimuthConverterTests
{
    public static TheoryData<string, int> KnownDirections => new()
    {
        { "северный", 0 },
        { "северо-восточный", 45 },
        { "восточный", 90 },
        { "юго-восточный", 135 },
        { "южный", 180 },
        { "юго-западный", 225 },
        { "западный", 270 },
        { "северо-западный", 315 },
    };

    [Theory]
    [MemberData(nameof(KnownDirections))]
    public void FromString_ReturnsExpectedAzimuth(string direction, int expectedAzimuth)
    {
        Assert.Equal(expectedAzimuth, WindDirectionAzimuthConverter.FromString(direction));
    }

    [Theory]
    [MemberData(nameof(KnownDirections))]
    public void FromString_IsCaseInsensitive(string direction, int expectedAzimuth)
    {
        Assert.Equal(expectedAzimuth, WindDirectionAzimuthConverter.FromString(direction.ToUpperInvariant()));
    }

    [Fact]
    public void FromString_ThrowsForEmptyValue()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => WindDirectionAzimuthConverter.FromString("  "));
        Assert.Contains("empty or null", exception.Message);
    }

    [Fact]
    public void FromString_ThrowsForUnknownValue()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => WindDirectionAzimuthConverter.FromString("unknown"));
        Assert.Contains("Unknown wind direction", exception.Message);
    }

    [Fact]
    public void GetAllKnownDirections_ReturnsEightDirectionsSortedAlphabetically()
    {
        var directions = WindDirectionAzimuthConverter.GetAllKnownDirections();

        Assert.Equal(8, directions.Count);
        Assert.Equal(directions, directions.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList());
    }

    [Fact]
    public void GetAllKnownDirectionMappings_ReturnsEightMappingsSortedByAzimuth()
    {
        var mappings = WindDirectionAzimuthConverter.GetAllKnownDirectionMappings();

        Assert.Equal(8, mappings.Count);
        Assert.Equal(mappings, mappings.OrderBy(pair => pair.Azimuth).ToList());
    }
}
