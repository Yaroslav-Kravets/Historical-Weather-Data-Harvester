// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

public sealed class CsvTreeComparison
{
    public CsvTreeComparison(
        string leftDir,
        string rightDir,
        int leftCsvCount,
        int rightCsvCount,
        IReadOnlyList<MatchedCsvPair> matchedPairs,
        IReadOnlyList<string> leftOnlyRelativePaths,
        IReadOnlyList<string> rightOnlyRelativePaths)
    {
        this.LeftDir = leftDir;
        this.RightDir = rightDir;
        this.LeftCsvCount = leftCsvCount;
        this.RightCsvCount = rightCsvCount;
        this.MatchedPairs = matchedPairs;
        this.LeftOnlyRelativePaths = leftOnlyRelativePaths;
        this.RightOnlyRelativePaths = rightOnlyRelativePaths;
        this.ContentIdenticalPairs = matchedPairs.Where(static pair => pair.ContentIdentical).ToArray();
        this.ContentDifferentPairs = matchedPairs.Where(static pair => !pair.ContentIdentical).ToArray();
        this.PartlyEqualPairs = this.ContentDifferentPairs.Where(static pair => pair.IsPartlyEqual).ToArray();
        this.DifferentPairs = this.ContentDifferentPairs.Where(static pair => !pair.IsPartlyEqual).ToArray();
    }

    public string LeftDir { get; }

    public string RightDir { get; }

    public int LeftCsvCount { get; }

    public int RightCsvCount { get; }

    public IReadOnlyList<MatchedCsvPair> MatchedPairs { get; }

    public IReadOnlyList<string> LeftOnlyRelativePaths { get; }

    public IReadOnlyList<string> RightOnlyRelativePaths { get; }

    public IReadOnlyList<MatchedCsvPair> ContentIdenticalPairs { get; }

    public IReadOnlyList<MatchedCsvPair> ContentDifferentPairs { get; }

    public IReadOnlyList<MatchedCsvPair> PartlyEqualPairs { get; }

    public IReadOnlyList<MatchedCsvPair> DifferentPairs { get; }

    public bool IsEqual =>
        this.LeftOnlyRelativePaths.Count == 0
        && this.RightOnlyRelativePaths.Count == 0
        && this.ContentDifferentPairs.Count == 0;
}
