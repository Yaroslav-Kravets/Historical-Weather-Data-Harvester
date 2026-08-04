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

/// <summary>
/// CSV row shape for normalized-column weather files.
/// </summary>
/// <remarks>
/// Always register <see cref="WeatherDataCsvRecordMap"/> on the CsvHelper context when
/// reading or writing this type. Without that ClassMap, <see cref="WeatherCharacteristics"/>
/// falls back to enum member names (e.g. <c>FreezingRain</c>) instead of English display
/// names (e.g. <c>Freezing Rain</c>).
/// </remarks>
public sealed class WeatherDataCsvRecord
{
    public WeatherDataCsvRecord()
    {
    }

    public WeatherDataCsvRecord(WeatherDataRow row)
    {
        this.Time = row.Time;
        this.Temperature = row.Temperature;
        this.WindDirection = row.WindDirectionAzimuth;
        this.WindSpeed = row.WindSpeed;
        this.AtmosphericPressure = row.AtmosphericPressure;
        this.Humidity = row.Humidity;
        this.WeatherCharacteristics = row.WeatherCharacteristics;
    }

    [Name(WeatherScalarCsvColumns.DateTime)]
    [Format(WeatherScalarCsvColumns.DateTimeFormat)]
    public DateTime Time { get; init; }

    [Name(WeatherScalarCsvColumns.Temperature)]
    public int Temperature { get; init; }

    /// <summary>
    /// Gets wind direction azimuth in degrees (0..359), not a compass label.
    /// </summary>
    [Name(WeatherScalarCsvColumns.WindDirection)]
    public int WindDirection { get; init; }

    [Name(WeatherScalarCsvColumns.WindSpeed)]
    public decimal WindSpeed { get; init; }

    [Name(WeatherScalarCsvColumns.AtmosphericPressure)]
    public int AtmosphericPressure { get; init; }

    [Name(WeatherScalarCsvColumns.Humidity)]
    public int Humidity { get; init; }

    /// <summary>
    /// Gets weather characteristic flags.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="WeatherDataCsvRecordMap"/> for English display-name serialization.
    /// </remarks>
    [Name(NormalizedWeatherCsvColumns.WeatherCharacteristics)]
    public WeatherCharacteristics WeatherCharacteristics { get; init; }
}
