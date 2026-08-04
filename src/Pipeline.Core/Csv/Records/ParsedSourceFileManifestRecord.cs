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

public sealed class ParsedSourceFileManifestRecord
{
    [Name(WeatherManifestCsvColumns.Place)]
    public string Place { get; init; } = string.Empty;

    [Name(WeatherManifestCsvColumns.Date)]
    [Format("yyyy-MM-dd")]
    public DateTime Date { get; init; }

    [Name(WeatherManifestCsvColumns.SourceFilePath)]
    public string SourceFilePath { get; init; } = string.Empty;
}
