// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Records;

public static class WeatherDataCsvRecordMapper
{
    public static WeatherDataRow ToRow(WeatherDataCsvRecord record)
    {
        return new WeatherDataRow(
            record.Time,
            record.WeatherCharacteristics,
            record.Temperature,
            record.WindDirection,
            record.WindSpeed,
            record.AtmosphericPressure,
            record.Humidity);
    }

    public static WeatherDataCsvRecord ToRecord(WeatherDataRow row) => new(row);
}
