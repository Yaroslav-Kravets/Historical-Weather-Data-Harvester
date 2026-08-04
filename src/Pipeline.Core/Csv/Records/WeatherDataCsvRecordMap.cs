// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Records;

using System.Globalization;
using Common;
using CsvHelper.Configuration;

public sealed class WeatherDataCsvRecordMap : ClassMap<WeatherDataCsvRecord>
{
    public WeatherDataCsvRecordMap(WeatherCharacteristicsEnglishCsvConverter characteristicsConverter)
    {
        Argument.ThrowIfNull(characteristicsConverter);

        this.AutoMap(CultureInfo.InvariantCulture);
        this.Map(static record => record.WeatherCharacteristics)
            .TypeConverter(characteristicsConverter);
    }
}
