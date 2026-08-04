// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer.Tests;

using Xunit;

public sealed class TimeNormalizationIssueCollectorTests
{
    [Fact]
    public void ConcurrentAdds_AreAllRecorded()
    {
        var collector = new TimeNormalizationIssueCollector();

        Parallel.For(0, 100, i =>
        {
            collector.AddMissingTimeEntry("Kyiv");
            collector.AddTimeNormalizationFailure("Kyiv");
        });

        var snapshot = collector.GetSnapshot();
        Assert.Equal(100, snapshot["Kyiv"].MissingTimeEntries);
        Assert.Equal(100, snapshot["Kyiv"].TimeNormalizationFailures);
    }
}
