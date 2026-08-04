// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Records;

using CsvHelper.Configuration.Attributes;

public sealed class ManifestCsvRecord
{
    public ManifestCsvRecord()
    {
    }

    public ManifestCsvRecord(string englishName, string nameInHtml)
    {
        this.EnglishName = englishName;
        this.NameInHtml = nameInHtml;
    }

    [Name(WeatherManifestCsvColumns.EnglishName)]
    public string EnglishName { get; init; } = string.Empty;

    [Name(WeatherManifestCsvColumns.NameInHtml)]
    public string NameInHtml { get; init; } = string.Empty;
}
