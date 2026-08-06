// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Paths;

using Xunit;

public sealed class WeatherCsvOutputPathsTests
{
    [Fact]
    public void StageDirectoryNames_AreExpectedValues()
    {
        Assert.Equal("parsed", WeatherCsvOutputPaths.ParsedStageDirectoryName);
        Assert.Equal("time-normalized", WeatherCsvOutputPaths.TimeNormalizedStageDirectoryName);
        Assert.Equal("normalized-columns", WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);
    }

    [Fact]
    public void ManifestFileNames_AreDistinctAndNonEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(WeatherCsvOutputPaths.ParsedSourceFilesManifestFileName));
        Assert.False(string.IsNullOrWhiteSpace(WeatherCsvOutputPaths.ParsedPlacesManifestFileName));
        Assert.False(string.IsNullOrWhiteSpace(WeatherCsvOutputPaths.WeatherCharacteristicsManifestFileName));
        Assert.False(string.IsNullOrWhiteSpace(WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName));

        var manifestFileNames = new[]
        {
            WeatherCsvOutputPaths.ParsedSourceFilesManifestFileName,
            WeatherCsvOutputPaths.ParsedPlacesManifestFileName,
            WeatherCsvOutputPaths.WeatherCharacteristicsManifestFileName,
        };

        Assert.Equal(manifestFileNames.Length, manifestFileNames.Distinct(StringComparer.Ordinal).Count());
        Assert.False(
            WeatherCsvOutputPaths.IsParsedStageManifestFileName(
                WeatherCsvOutputPaths.WeatherCharacteristicsUsageFileName));
    }
}
