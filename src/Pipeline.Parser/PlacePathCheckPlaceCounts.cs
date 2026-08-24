// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

public sealed class PlacePathCheckPlaceCounts
{
    private int matches;
    private int mismatches;

    public PlacePathCheckPlaceCounts(string place)
    {
        this.Place = place;
    }

    public string Place { get; }

    public int Matches => this.matches;

    public int Mismatches => this.mismatches;

    public int FilesChecked => this.matches + this.mismatches;

    public void IncrementMatches() => Interlocked.Increment(ref this.matches);

    public void IncrementMismatches() => Interlocked.Increment(ref this.mismatches);
}
