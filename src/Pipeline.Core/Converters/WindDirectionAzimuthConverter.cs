// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Converters;

public static class WindDirectionAzimuthConverter
{
    // Maps normalized NameInHtml wind direction names to azimuth angles (degrees, 0..359)
    private static readonly (string Name, int Azimuth)[] OrderedPairs =
    {
        ("северный", 0),
        ("северо-восточный", 45),
        ("восточный", 90),
        ("юго-восточный", 135),
        ("южный", 180),
        ("юго-западный", 225),
        ("западный", 270),
        ("северо-западный", 315),
    };

    private static readonly Dictionary<string, int> NameToAzimuthMap =
        OrderedPairs.ToDictionary(pair => pair.Name, pair => pair.Azimuth, StringComparer.OrdinalIgnoreCase);

    public static int FromString(string value, string? context = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            var location = string.IsNullOrEmpty(context) ? string.Empty : $" in {context}";
            throw new InvalidOperationException($"Wind direction is empty or null{location}.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (NameToAzimuthMap.TryGetValue(normalized, out var azimuth))
        {
            return azimuth;
        }

        var locationSuffix = string.IsNullOrEmpty(context) ? string.Empty : $" in {context}";
        throw new InvalidOperationException($"Unknown wind direction '{value}'{locationSuffix}.");
    }

    public static IReadOnlyList<string> GetAllKnownDirections()
    {
        return OrderedPairs
            .Select(pair => pair.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<(string Name, int Azimuth)> GetAllKnownDirectionMappings()
    {
        return OrderedPairs
            .OrderBy(pair => pair.Azimuth)
            .ToList();
    }
}
