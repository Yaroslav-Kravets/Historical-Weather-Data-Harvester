// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

internal sealed class PlacePathCheckPlaceCounts
{
    private int matches;
    private int mismatches;

    internal PlacePathCheckPlaceCounts(string place)
    {
        this.Place = place;
    }

    public string Place { get; }

    public int Matches => Volatile.Read(ref this.matches);

    public int Mismatches => Volatile.Read(ref this.mismatches);

    public int FilesChecked => this.Matches + this.Mismatches;

    internal void IncrementMatches() => Interlocked.Increment(ref this.matches);

    internal void IncrementMismatches() => Interlocked.Increment(ref this.mismatches);
}
