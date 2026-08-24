// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using System.Collections.Concurrent;
using Common;

public sealed class ParsingIssueCollector
{
    public const string UnknownPlace = "(Unknown)";

    private readonly ConcurrentDictionary<string, ParsingPlaceErrorCounts> countsByPlace =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, PlacePathCheckPlaceCounts> placePathCheckCountsByPlace =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentBag<PlacePathMismatch> pathPlaceMismatches = new();
    private readonly PlaceConverter placeConverter;

    public ParsingIssueCollector(PlaceConverter placeConverter)
    {
        Argument.ThrowIfNull(placeConverter);

        this.placeConverter = placeConverter;
    }

    public void AddParseFailure(string filePath)
    {
        var place = this.ResolveDisplayNameFromFilePath(filePath);
        this.GetOrAdd(place).IncrementParseFailures();
    }

    public void AddSkippedFile(string placeDisplayName)
    {
        this.GetOrAdd(placeDisplayName).IncrementSkippedFiles();
    }

    public void AddDuplicateDate(string placeDisplayName)
    {
        this.GetOrAdd(placeDisplayName).IncrementDuplicateDates();
    }

    public void AddPathPlaceMismatch(
        string filePath,
        string pathPlaceDisplay,
        string htmlCityName,
        string htmlPlaceDisplay)
    {
        this.GetOrAdd(htmlPlaceDisplay).IncrementPathPlaceMismatches();
        this.GetOrAddPlacePathCheckCounts(htmlPlaceDisplay).IncrementMismatches();

        this.pathPlaceMismatches.Add(new PlacePathMismatch(
            filePath,
            pathPlaceDisplay,
            htmlCityName,
            htmlPlaceDisplay));
    }

    public void AddPathPlaceMatch(string htmlPlaceDisplay)
    {
        this.GetOrAddPlacePathCheckCounts(htmlPlaceDisplay).IncrementMatches();
    }

    public IReadOnlyList<PlacePathMismatch> GetPathPlaceMismatches() =>
        this.pathPlaceMismatches
            .OrderBy(entry => entry.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public PlacePathCheckTotals GetPlacePathCheckTotals()
    {
        var placeCounts = this.placePathCheckCountsByPlace.Values.ToList();
        var matches = placeCounts.Sum(counts => counts.Matches);
        var mismatches = placeCounts.Sum(counts => counts.Mismatches);
        return new PlacePathCheckTotals(matches + mismatches, matches, mismatches);
    }

    /// <summary>
    /// Groups path-place check counts by HTML-derived place name.
    /// </summary>
    /// <returns>Per-place path-place check summaries ordered by place name.</returns>
    public IReadOnlyList<PlacePathCheckPlaceSummary> GetPlacePathCheckSummaryByPlace() =>
        this.placePathCheckCountsByPlace.Values
            .Select(counts => new PlacePathCheckPlaceSummary(
                counts.Place,
                counts.FilesChecked,
                counts.Matches,
                counts.Mismatches))
            .OrderBy(summary => summary.Place, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IReadOnlyDictionary<string, ParsingPlaceErrorCounts> GetSnapshot()
    {
        return this.countsByPlace.ToDictionary(
            pair => pair.Key,
            pair => Clone(pair.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    private static ParsingPlaceErrorCounts Clone(ParsingPlaceErrorCounts source)
    {
        var clone = new ParsingPlaceErrorCounts(source.Place);
        clone.CopyCountsFrom(source);
        return clone;
    }

    private string ResolveDisplayNameFromFilePath(string filePath)
    {
        Argument.ThrowIfNull(filePath);
        if (this.placeConverter.TryFromFilePath(filePath, out var place))
        {
            return this.placeConverter.ToDisplayName(place);
        }

        return UnknownPlace;
    }

    private ParsingPlaceErrorCounts GetOrAdd(string place)
    {
        return this.countsByPlace.GetOrAdd(place, static key => new ParsingPlaceErrorCounts(key));
    }

    private PlacePathCheckPlaceCounts GetOrAddPlacePathCheckCounts(string place)
    {
        return this.placePathCheckCountsByPlace.GetOrAdd(place, static key => new PlacePathCheckPlaceCounts(key));
    }
}
