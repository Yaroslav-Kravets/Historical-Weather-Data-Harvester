// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer;

public sealed class TimeNormalizingPlaceErrorCountsBuilder
{
    public IReadOnlyList<TimeNormalizingPlaceErrorCounts> Build(
        IEnumerable<string> knownPlaces,
        TimeNormalizationIssueCollector collector,
        IReadOnlyDictionary<string, PlaceTimeNormalizationCounts> timeNormalizationCounts)
    {
        var countsByPlace = new Dictionary<string, TimeNormalizingPlaceErrorCounts>(StringComparer.OrdinalIgnoreCase);

        foreach (var place in knownPlaces)
        {
            countsByPlace[place] = new TimeNormalizingPlaceErrorCounts(place);
        }

        foreach (var (place, collectedCounts) in collector.GetSnapshot())
        {
            if (!countsByPlace.TryGetValue(place, out var mergedCounts))
            {
                mergedCounts = new TimeNormalizingPlaceErrorCounts(place);
                countsByPlace[place] = mergedCounts;
            }

            mergedCounts.CopyCountsFrom(collectedCounts);
        }

        foreach (var (place, placeCounts) in timeNormalizationCounts)
        {
            if (!countsByPlace.TryGetValue(place, out var mergedCounts))
            {
                mergedCounts = new TimeNormalizingPlaceErrorCounts(place);
                countsByPlace[place] = mergedCounts;
            }

            if (placeCounts.MissingTimeEntries > mergedCounts.MissingTimeEntries)
            {
                mergedCounts.SetMissingTimeEntries(placeCounts.MissingTimeEntries);
            }

            if (placeCounts.Unsuccessful > mergedCounts.TimeNormalizationFailures)
            {
                mergedCounts.SetTimeNormalizationFailures(placeCounts.Unsuccessful);
            }
        }

        return countsByPlace.Values
            .OrderBy(counts => counts.Place, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
