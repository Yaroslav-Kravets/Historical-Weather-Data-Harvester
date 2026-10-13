// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

public sealed class ParsingPlaceErrorCounts
{
    private int parseFailures;
    private int skippedFiles;
    private int duplicateDates;
    private int placePathMismatches;

    public ParsingPlaceErrorCounts(string place)
    {
        this.Place = place;
    }

    public string Place { get; }

    public int ParseFailures => Volatile.Read(ref this.parseFailures);

    public int SkippedFiles => Volatile.Read(ref this.skippedFiles);

    public int DuplicateDates => Volatile.Read(ref this.duplicateDates);

    public int PlacePathMismatches => Volatile.Read(ref this.placePathMismatches);

    public int TotalIssues => this.ParseFailures + this.SkippedFiles + this.DuplicateDates + this.PlacePathMismatches;

    internal void IncrementParseFailures() => Interlocked.Increment(ref this.parseFailures);

    internal void IncrementSkippedFiles() => Interlocked.Increment(ref this.skippedFiles);

    internal void IncrementDuplicateDates() => Interlocked.Increment(ref this.duplicateDates);

    internal void IncrementPlacePathMismatches() => Interlocked.Increment(ref this.placePathMismatches);

    internal void CopyCountsFrom(ParsingPlaceErrorCounts source)
    {
        Volatile.Write(ref this.parseFailures, source.ParseFailures);
        Volatile.Write(ref this.skippedFiles, source.SkippedFiles);
        Volatile.Write(ref this.duplicateDates, source.DuplicateDates);
        Volatile.Write(ref this.placePathMismatches, source.PlacePathMismatches);
    }
}
