// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Paths;

/// <summary>
/// Directory and file names used by pipeline stage output folders.
/// </summary>
public static class WeatherCsvOutputPaths
{
    public const string ParsedStageDirectoryName = "parsed";

    public const string TimeNormalizedStageDirectoryName = "time-normalized";

    public const string NormalizedColumnsDirectoryName = "normalized-columns";

    public const string ParsedSourceFilesManifestFileName = "parsed-source-files.csv";

    public const string ParsedPlacesManifestFileName = "parsed-places.csv";

    public const string WeatherCharacteristicsManifestFileName = "weather-characteristics.csv";

    public static bool IsParsedStageManifestFileName(string fileName) =>
        fileName.Equals(ParsedSourceFilesManifestFileName, StringComparison.OrdinalIgnoreCase)
        || fileName.Equals(ParsedPlacesManifestFileName, StringComparison.OrdinalIgnoreCase)
        || fileName.Equals(WeatherCharacteristicsManifestFileName, StringComparison.OrdinalIgnoreCase);
}
