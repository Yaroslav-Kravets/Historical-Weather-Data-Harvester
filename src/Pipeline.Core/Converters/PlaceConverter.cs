// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Converters;

using System.Reflection;
using Common;

/// <summary>
/// Maps place identifiers between original NameInHtml, English display names, and path aliases.
/// </summary>
public sealed class PlaceConverter
{
    private readonly TranslationMaps maps;

    public PlaceConverter()
    {
        this.maps = BuildTranslationMaps();
    }

    /// <summary>
    /// Resolves a place from its original NameInHtml as scraped from HTML.
    /// </summary>
    /// <returns>The resolved <see cref="Place"/>.</returns>
    public Place FromNameInHtml(string nameInHtml, string? context = null)
    {
        Argument.ThrowIf(
            nameInHtml,
            string.IsNullOrWhiteSpace,
            "Place name is empty or null.",
            nameof(nameInHtml));

        if (!this.TryFromNameInHtml(nameInHtml, out var place))
        {
            var trimmed = nameInHtml.Trim();
            var location = string.IsNullOrEmpty(context) ? string.Empty : $" in {context}";
            throw new InvalidOperationException($"Unknown place '{trimmed}'{location}.");
        }

        return place;
    }

    /// <summary>
    /// Attempts to resolve a place from its original NameInHtml as scraped from HTML.
    /// </summary>
    /// <returns><see langword="true"/> if the name maps to a known place; otherwise <see langword="false"/>.</returns>
    public bool TryFromNameInHtml(string? nameInHtml, out Place place)
    {
        place = default;

        if (string.IsNullOrWhiteSpace(nameInHtml))
        {
            return false;
        }

        return this.maps.NameInHtmlToPlaceMap.TryGetValue(nameInHtml.Trim(), out place);
    }

    /// <summary>
    /// Returns the primary NameInHtml for the place.
    /// </summary>
    /// <returns>The primary NameInHtml for the place.</returns>
    public string ToNameInHtml(Place place)
    {
        if (!this.maps.PlaceToPrimaryNameInHtmlMap.TryGetValue(place, out var nameInHtml))
        {
            throw new ArgumentException($"Place.{place} has no entry in PlaceConverter.", nameof(place));
        }

        return nameInHtml;
    }

    /// <summary>
    /// Attempts to resolve a place from an English alias segment in <paramref name="filePath"/>.
    /// Uses <see cref="Path"/> string helpers (not <c>IFileSystem</c>) because only path text is inspected.
    /// </summary>
    /// <param name="filePath">
    /// A host absolute path (directory source) or an archive-relative entry key (`.7z` source).
    /// Resolution walks directory segments, so both shapes work when a known place folder appears in the path.
    /// </param>
    /// <param name="place">
    /// When this method returns, contains the resolved place if a matching segment was found;
    /// otherwise, the default value for <see cref="Place"/>.
    /// </param>
    /// <returns><see langword="true"/> if a directory segment maps to a known place; otherwise <see langword="false"/>.</returns>
    public bool TryFromFilePath(string filePath, out Place place)
    {
        place = default;

        foreach (var segment in EnumerateDirectorySegments(filePath))
        {
            if (this.maps.EnglishAliasToPlaceMap.TryGetValue(segment, out place))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the English display name used for CSV filenames and manifests.
    /// </summary>
    /// <returns>The English display name for the place.</returns>
    public string ToDisplayName(Place place)
    {
        if (!this.maps.PlaceToDisplayNameMap.TryGetValue(place, out var displayName))
        {
            throw new ArgumentException($"Place.{place} has no display name in PlaceConverter.", nameof(place));
        }

        return displayName;
    }

    /// <summary>
    /// Returns all (NameInHtml, Place) pairs discovered from enum attributes.
    /// </summary>
    /// <returns>All (NameInHtml, Place) pairs from enum attributes.</returns>
    public IReadOnlyList<(string NameInHtml, Place Place)> GetAllPairs() => this.maps.OrderedPairs;

    /// <summary>
    /// Resolves the English display name for the original NameInHtml.
    /// </summary>
    /// <returns>The English display name for the original NameInHtml.</returns>
    public string ToDisplayNameFromNameInHtml(string nameInHtml, string? context = null) =>
        this.ToDisplayName(this.FromNameInHtml(nameInHtml, context));

    private static IEnumerable<string> EnumerateDirectorySegments(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            yield break;
        }

        var directoryPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            yield break;
        }

        var segments = directoryPath.Split('/', '\\');
        for (var i = segments.Length - 1; i >= 0; i--)
        {
            var segment = segments[i].Trim();
            if (segment.Length > 0)
            {
                yield return segment;
            }
        }
    }

    private static TranslationMaps BuildTranslationMaps()
    {
        var orderedPairs = EnumNamingReflection.BuildNameInHtmlPairs<Place>();
        var nameInHtmlToPlaceMap = new Dictionary<string, Place>(StringComparer.OrdinalIgnoreCase);
        var placeToPrimaryNameInHtmlMap = new Dictionary<Place, string>();

        foreach (var (nameInHtml, place) in orderedPairs)
        {
            AddUnique(nameInHtmlToPlaceMap, nameInHtml, place, "NameInHtml");
            placeToPrimaryNameInHtmlMap[place] = nameInHtml;
        }

        var (placeToDisplayNameMap, englishAliasToPlaceMap) = BuildDisplayNameAndAliasMaps();

        return new TranslationMaps(
            orderedPairs,
            nameInHtmlToPlaceMap,
            placeToPrimaryNameInHtmlMap,
            placeToDisplayNameMap,
            englishAliasToPlaceMap);
    }

    private static (Dictionary<Place, string> PlaceToDisplayName, Dictionary<string, Place> EnglishAliasToPlace)
        BuildDisplayNameAndAliasMaps()
    {
        var placeToDisplayName = new Dictionary<Place, string>();
        var displayNameToPlace = new Dictionary<string, Place>(StringComparer.OrdinalIgnoreCase);
        var englishAliasToPlace = new Dictionary<string, Place>(StringComparer.OrdinalIgnoreCase);

        foreach (var place in Enum.GetValues<Place>())
        {
            var field = GetPlaceField(place);
            var displayName = EnumDisplayNameFormatter.ToDisplayName(place);
            var enumMemberName = place.ToString();

            AddUnique(displayNameToPlace, displayName, place, "English display name");
            placeToDisplayName[place] = displayName;

            AddUnique(englishAliasToPlace, enumMemberName, place, "English alias");

            if (!string.Equals(displayName, enumMemberName, StringComparison.OrdinalIgnoreCase))
            {
                AddUnique(englishAliasToPlace, displayName, place, "English alias");
            }

            foreach (var alternate in field.GetCustomAttributes<AlternateNameAttribute>())
            {
                Argument.ThrowIf(
                    alternate.Name,
                    string.IsNullOrWhiteSpace,
                    $"Place.{place} has an empty {nameof(AlternateNameAttribute)}.{nameof(AlternateNameAttribute.Name)}.",
                    nameof(AlternateNameAttribute.Name));

                AddUnique(englishAliasToPlace, alternate.Name, place, "English alias");
            }
        }

        return (placeToDisplayName, englishAliasToPlace);
    }

    private static void AddUnique(
        Dictionary<string, Place> map,
        string key,
        Place place,
        string description)
    {
        if (!map.TryAdd(key, place))
        {
            throw new InvalidOperationException(
                $"Duplicate {description} '{key}' for Place.{place} and Place.{map[key]}.");
        }
    }

    private static FieldInfo GetPlaceField(Place place)
    {
        var field = typeof(Place).GetField(place.ToString(), BindingFlags.Public | BindingFlags.Static);
        if (field is null)
        {
            throw new InvalidOperationException($"Place.{place} has no reflection field.");
        }

        return field;
    }

    private sealed record TranslationMaps(
        (string NameInHtml, Place Place)[] OrderedPairs,
        Dictionary<string, Place> NameInHtmlToPlaceMap,
        Dictionary<Place, string> PlaceToPrimaryNameInHtmlMap,
        Dictionary<Place, string> PlaceToDisplayNameMap,
        Dictionary<string, Place> EnglishAliasToPlaceMap);
}
