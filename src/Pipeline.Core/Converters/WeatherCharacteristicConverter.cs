// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Converters;

using Common;

/// <summary>
/// Maps weather characteristic flags between original NameInHtml strings and English CSV cells.
/// </summary>
public sealed class WeatherCharacteristicConverter
{
    private readonly TranslationMaps maps;

    public WeatherCharacteristicConverter()
    {
        this.maps = BuildTranslationMaps();
    }

    /// <summary>
    /// Aggregates characteristic flags from original NameInHtml characteristic names.
    /// </summary>
    /// <returns>Aggregated <see cref="WeatherCharacteristics"/> flags for the given NameInHtml values.</returns>
    public WeatherCharacteristics FromStrings(IEnumerable<string> values, string? context = null)
    {
        Argument.ThrowIfNull(values);
        var aggregate = WeatherCharacteristics.None;
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            if (!this.maps.NameInHtmlToFlagMap.TryGetValue(trimmed, out var flag))
            {
                var location = string.IsNullOrEmpty(context) ? string.Empty : $" in {context}";
                throw new InvalidOperationException($"Unknown weather characteristic '{trimmed}'{location}.");
            }

            aggregate |= flag;
        }

        return aggregate;
    }

    /// <summary>
    /// Returns the NameInHtml values for the set bits in <paramref name="characteristics"/>.
    /// </summary>
    /// <returns>NameInHtml values for the set bits.</returns>
    public IReadOnlyList<string> ToStrings(WeatherCharacteristics characteristics)
    {
        if (characteristics == WeatherCharacteristics.None)
        {
            return Array.Empty<string>();
        }

        var results = new List<string>();
        foreach (var flag in this.EnumerateSetFlags(characteristics))
        {
            results.Add(this.maps.FlagToNameInHtmlMap[flag]);
        }

        return results;
    }

    /// <summary>
    /// Returns all known NameInHtml characteristic names, sorted alphabetically.
    /// </summary>
    /// <returns>All known NameInHtml characteristic names, sorted alphabetically.</returns>
    public IReadOnlyList<string> GetAllKnownCharacteristics()
    {
        return this.maps.OrderedPairs
            .Select(pair => pair.NameInHtml)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Returns all (NameInHtml, Flag) pairs discovered from enum attributes.
    /// </summary>
    /// <returns>All (NameInHtml, Flag) pairs from enum attributes.</returns>
    public IReadOnlyList<(string NameInHtml, WeatherCharacteristics Flag)> GetAllPairs() =>
        this.maps.OrderedPairs;

    /// <summary>
    /// Returns (EnglishName, NameInHtml) pairs for the set bits in
    /// <paramref name="observedFlags"/>, sorted by English display name.
    /// </summary>
    /// <returns>(EnglishName, NameInHtml) pairs for the observed flags, sorted by English name.</returns>
    public IReadOnlyList<(string EnglishName, string NameInHtml)> GetObservedPairs(
        WeatherCharacteristics observedFlags)
    {
        if (observedFlags == WeatherCharacteristics.None)
        {
            return Array.Empty<(string EnglishName, string NameInHtml)>();
        }

        return this.EnumerateSetFlags(observedFlags)
            .Select(flag => (
                EnglishName: EnumDisplayNameFormatter.ToDisplayName(flag),
                NameInHtml: this.maps.FlagToNameInHtmlMap[flag]))
            .OrderBy(pair => pair.EnglishName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Formats flags as a comma-separated English display-name cell for normalized-column CSV.
    /// </summary>
    /// <returns>Comma-separated English display names, or empty when <see cref="WeatherCharacteristics.None"/>.</returns>
    public string ToEnglishCsvCell(WeatherCharacteristics characteristics)
    {
        if (characteristics == WeatherCharacteristics.None)
        {
            return string.Empty;
        }

        var labels = this.EnumerateSetFlags(characteristics)
            .Select(EnumDisplayNameFormatter.ToDisplayName)
            .ToList();

        labels.Sort(StringComparer.OrdinalIgnoreCase);
        return string.Join(", ", labels);
    }

    /// <summary>
    /// Parses a comma-separated English display-name cell from normalized-column CSV.
    /// </summary>
    /// <returns>Parsed <see cref="WeatherCharacteristics"/> flags from the CSV cell.</returns>
    public WeatherCharacteristics FromEnglishCsvCell(string? cell)
    {
        if (string.IsNullOrWhiteSpace(cell))
        {
            return WeatherCharacteristics.None;
        }

        var aggregate = WeatherCharacteristics.None;

        foreach (var part in cell.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!this.maps.EnglishDisplayNameToFlagMap.TryGetValue(part, out var flag))
            {
                throw new InvalidOperationException($"Unknown weather characteristic '{part}' in CSV cell.");
            }

            aggregate |= flag;
        }

        return aggregate;
    }

    private static TranslationMaps BuildTranslationMaps()
    {
        var orderedPairs = EnumNamingReflection.BuildNameInHtmlPairsExcludingNone<WeatherCharacteristics>();
        var nameInHtmlToFlagMap = new Dictionary<string, WeatherCharacteristics>(StringComparer.OrdinalIgnoreCase);
        var flagToNameInHtmlMap = new Dictionary<WeatherCharacteristics, string>();
        var englishDisplayNameToFlagMap =
            new Dictionary<string, WeatherCharacteristics>(StringComparer.OrdinalIgnoreCase);

        foreach (var (nameInHtml, flag) in orderedPairs)
        {
            AddUnique(nameInHtmlToFlagMap, nameInHtml, flag, "NameInHtml");
            flagToNameInHtmlMap[flag] = nameInHtml;

            var englishDisplayName = EnumDisplayNameFormatter.ToDisplayName(flag);
            AddUnique(englishDisplayNameToFlagMap, englishDisplayName, flag, "English display name");
        }

        return new TranslationMaps(
            orderedPairs,
            nameInHtmlToFlagMap,
            flagToNameInHtmlMap,
            englishDisplayNameToFlagMap);
    }

    private static void AddUnique(
        Dictionary<string, WeatherCharacteristics> map,
        string key,
        WeatherCharacteristics flag,
        string description)
    {
        if (!map.TryAdd(key, flag))
        {
            throw new InvalidOperationException(
                $"Duplicate {description} '{key}' for WeatherCharacteristics.{flag} and WeatherCharacteristics.{map[key]}.");
        }
    }

    private IEnumerable<WeatherCharacteristics> EnumerateSetFlags(WeatherCharacteristics characteristics)
    {
        foreach (var (_, flag) in this.maps.OrderedPairs)
        {
            if ((characteristics & flag) == flag)
            {
                yield return flag;
            }
        }
    }

    private sealed record TranslationMaps(
        (string NameInHtml, WeatherCharacteristics Flag)[] OrderedPairs,
        Dictionary<string, WeatherCharacteristics> NameInHtmlToFlagMap,
        Dictionary<WeatherCharacteristics, string> FlagToNameInHtmlMap,
        Dictionary<string, WeatherCharacteristics> EnglishDisplayNameToFlagMap);
}
