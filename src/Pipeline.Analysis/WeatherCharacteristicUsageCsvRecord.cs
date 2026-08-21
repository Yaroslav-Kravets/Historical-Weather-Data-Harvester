// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis;

using CsvHelper.Configuration.Attributes;

public sealed class WeatherCharacteristicUsageCsvRecord
{
    public WeatherCharacteristicUsageCsvRecord()
    {
    }

    public WeatherCharacteristicUsageCsvRecord(
        string englishName,
        string nameInHtml,
        int rowCount,
        string percentOfRows)
    {
        this.EnglishName = englishName;
        this.NameInHtml = nameInHtml;
        this.RowCount = rowCount;
        this.PercentOfRows = percentOfRows;
    }

    [Name("EnglishName")]
    public string EnglishName { get; init; } = string.Empty;

    [Name("NameInHtml")]
    public string NameInHtml { get; init; } = string.Empty;

    [Name("RowCount")]
    public int RowCount { get; init; }

    [Name("PercentOfRows")]
    public string PercentOfRows { get; init; } = string.Empty;
}
