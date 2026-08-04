// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer;

using System.Collections.Concurrent;

public sealed class TimeNormalizationIssueCollector
{
    private readonly ConcurrentDictionary<string, TimeNormalizingPlaceErrorCounts> countsByPlace =
        new(StringComparer.OrdinalIgnoreCase);

    public void AddMissingTimeEntry(string place)
    {
        this.GetOrAdd(place).IncrementMissingTimeEntries();
    }

    public void AddTimeNormalizationFailure(string place)
    {
        this.GetOrAdd(place).IncrementTimeNormalizationFailures();
    }

    public IReadOnlyDictionary<string, TimeNormalizingPlaceErrorCounts> GetSnapshot()
    {
        return this.countsByPlace.ToDictionary(
            pair => pair.Key,
            pair => Clone(pair.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    private static TimeNormalizingPlaceErrorCounts Clone(TimeNormalizingPlaceErrorCounts source)
    {
        var clone = new TimeNormalizingPlaceErrorCounts(source.Place);
        clone.CopyCountsFrom(source);
        return clone;
    }

    private TimeNormalizingPlaceErrorCounts GetOrAdd(string place)
    {
        return this.countsByPlace.GetOrAdd(place, static key => new TimeNormalizingPlaceErrorCounts(key));
    }
}
