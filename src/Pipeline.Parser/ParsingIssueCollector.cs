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

    private readonly ConcurrentBag<PlacePathSelfCheckEntry> pathSelfChecks = new();
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

        this.pathSelfChecks.Add(new PlacePathSelfCheckEntry(
            filePath,
            pathPlaceDisplay,
            htmlCityName,
            htmlPlaceDisplay,
            IsMatch: false));
    }

    public void AddPathPlaceMatch(
        string filePath,
        string pathPlaceDisplay,
        string htmlCityName,
        string htmlPlaceDisplay)
    {
        this.pathSelfChecks.Add(new PlacePathSelfCheckEntry(
            filePath,
            pathPlaceDisplay,
            htmlCityName,
            htmlPlaceDisplay,
            IsMatch: true));
    }

    public IReadOnlyList<PlacePathSelfCheckEntry> GetPathSelfChecks() =>
        this.pathSelfChecks
            .OrderBy(entry => entry.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public PathSelfCheckTotals GetPathSelfCheckTotals()
    {
        var entries = this.pathSelfChecks.ToList();
        var matches = entries.Count(entry => entry.IsMatch);
        return new PathSelfCheckTotals(entries.Count, matches, entries.Count - matches);
    }

    /// <summary>
    /// Groups self-check entries by HTML-derived place name (<see cref="PlacePathSelfCheckEntry.HtmlPlaceDisplay"/>).
    /// </summary>
    /// <returns>Per-place self-check summaries ordered by place name.</returns>
    public IReadOnlyList<PlacePathSelfCheckPlaceSummary> GetPathSelfCheckSummaryByPlace() =>
        this.pathSelfChecks
            .GroupBy(entry => entry.HtmlPlaceDisplay, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var matches = group.Count(entry => entry.IsMatch);
                var filesChecked = group.Count();
                return new PlacePathSelfCheckPlaceSummary(
                    group.Key,
                    filesChecked,
                    matches,
                    filesChecked - matches);
            })
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
}
