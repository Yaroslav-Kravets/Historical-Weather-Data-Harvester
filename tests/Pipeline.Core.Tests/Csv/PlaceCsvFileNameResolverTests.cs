// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv;

using Pipeline.Core.Tests.Csv.TestSupport;
using Xunit;

public sealed class PlaceCsvFileNameResolverTests
{
    private readonly CsvTestContext testContext;
    private readonly PlaceCsvFileNameResolver resolver;

    public PlaceCsvFileNameResolverTests()
    {
        this.testContext = new CsvTestContext();
        this.resolver = this.testContext.PlaceCsvFileNameResolver;
    }

    [Fact]
    public void ToCsvFileName_ReplacesInvalidFileNameCharacters()
    {
        var invalidChar = this.testContext.FileSystem.Path.GetInvalidFileNameChars().First(c => c != '\0');
        var fileName = this.resolver.ToCsvFileName($"bad{invalidChar}name");

        Assert.Equal("bad_name.csv", fileName);
    }

    [Fact]
    public void ToCsvFileName_ThrowsForEmptyPlaceName()
    {
        Assert.Throws<ArgumentException>(() => this.resolver.ToCsvFileName("  "));
    }

    [Fact]
    public void GetPlaceNameFromCsvFileName_ReturnsNameWithoutExtension()
    {
        Assert.Equal("Kyiv", this.resolver.GetPlaceNameFromCsvFileName("Kyiv.csv"));
    }
}
