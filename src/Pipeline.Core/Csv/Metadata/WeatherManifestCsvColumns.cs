// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Metadata;

public static class WeatherManifestCsvColumns
{
    public const string EnglishName = "EnglishName";
    public const string NameInHtml = "NameInHtml";
    public const string Place = "Place";
    public const string Date = "Date";
    public const string SourceFilePath = "SourceFilePath";

    public static readonly IReadOnlyList<string> ManifestColumns = new[]
    {
        EnglishName,
        NameInHtml,
    };
}
