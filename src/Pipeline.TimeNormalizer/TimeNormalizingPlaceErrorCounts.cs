// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer;

public sealed class TimeNormalizingPlaceErrorCounts
{
    private int missingTimeEntries;
    private int timeNormalizationFailures;

    public TimeNormalizingPlaceErrorCounts(string place)
    {
        this.Place = place;
    }

    public string Place { get; }

    public int MissingTimeEntries => Volatile.Read(ref this.missingTimeEntries);

    public int TimeNormalizationFailures => Volatile.Read(ref this.timeNormalizationFailures);

    public int TotalIssues => this.MissingTimeEntries + this.TimeNormalizationFailures;

    internal void IncrementMissingTimeEntries() => Interlocked.Increment(ref this.missingTimeEntries);

    internal void IncrementTimeNormalizationFailures() => Interlocked.Increment(ref this.timeNormalizationFailures);

    internal void CopyCountsFrom(TimeNormalizingPlaceErrorCounts source)
    {
        Volatile.Write(ref this.missingTimeEntries, source.MissingTimeEntries);
        Volatile.Write(ref this.timeNormalizationFailures, source.TimeNormalizationFailures);
    }

    internal void SetMissingTimeEntries(int value) => Volatile.Write(ref this.missingTimeEntries, value);

    internal void SetTimeNormalizationFailures(int value) => Volatile.Write(ref this.timeNormalizationFailures, value);
}
