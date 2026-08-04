// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv.Readers;

using Pipeline.Core.Tests.Csv.TestSupport;
using Xunit;

public sealed class WeatherDataRowsByDateGrouperTests
{
    [Fact]
    public void Group_ReturnsSortedDictionaryWithChronologicalDateKeys()
    {
        var rows = new[]
        {
            WeatherDataRowTestFactory.Create(new DateTime(2003, 1, 3, 12, 0, 0)),
            WeatherDataRowTestFactory.Create(new DateTime(2003, 1, 1, 0, 0, 0)),
            WeatherDataRowTestFactory.Create(new DateTime(2003, 1, 2, 6, 0, 0)),
            WeatherDataRowTestFactory.Create(new DateTime(2003, 1, 1, 3, 0, 0)),
        };

        var grouped = WeatherDataRowsByDateGrouper.Group(rows);

        Assert.IsType<SortedDictionary<DateTime, List<WeatherDataRow>>>(grouped);
        Assert.Equal(
            new[]
            {
                new DateTime(2003, 1, 1),
                new DateTime(2003, 1, 2),
                new DateTime(2003, 1, 3),
            },
            grouped.Keys.ToArray());
        Assert.Equal(2, grouped[new DateTime(2003, 1, 1)].Count);
        Assert.Equal(
            new[]
            {
                new DateTime(2003, 1, 1, 0, 0, 0),
                new DateTime(2003, 1, 1, 3, 0, 0),
            },
            grouped[new DateTime(2003, 1, 1)].Select(row => row.Time).ToArray());
    }
}
