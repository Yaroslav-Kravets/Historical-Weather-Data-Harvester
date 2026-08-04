// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

public sealed class ParsingPlaceErrorCountsBuilder
{
    public IReadOnlyList<ParsingPlaceErrorCounts> Build(
        IEnumerable<string> knownPlaces,
        ParsingIssueCollector collector)
    {
        var countsByPlace = new Dictionary<string, ParsingPlaceErrorCounts>(StringComparer.OrdinalIgnoreCase);

        foreach (var place in knownPlaces)
        {
            countsByPlace[place] = new ParsingPlaceErrorCounts(place);
        }

        foreach (var (place, collectedCounts) in collector.GetSnapshot())
        {
            if (!countsByPlace.TryGetValue(place, out var mergedCounts))
            {
                mergedCounts = new ParsingPlaceErrorCounts(place);
                countsByPlace[place] = mergedCounts;
            }

            mergedCounts.CopyCountsFrom(collectedCounts);
        }

        return countsByPlace.Values
            .OrderBy(counts => counts.Place == ParsingIssueCollector.UnknownPlace)
            .ThenBy(counts => counts.Place, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
