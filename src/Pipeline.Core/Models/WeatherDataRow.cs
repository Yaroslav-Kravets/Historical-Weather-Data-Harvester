// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Models;

/// <summary>
/// Represents a single row of weather data from the archive table.
/// </summary>
public sealed class WeatherDataRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherDataRow"/> class.
    /// </summary>
    /// <param name="time">The date and time value.</param>
    /// <param name="weatherCharacteristics">The weather characteristics flags.</param>
    /// <param name="temperature">The air temperature in degrees Celsius.</param>
    /// <param name="windDirectionAzimuth">The wind direction azimuth angle (0..359).</param>
    /// <param name="windSpeed">The wind speed in m/s.</param>
    /// <param name="atmosphericPressure">The atmospheric pressure.</param>
    /// <param name="humidity">The air humidity percentage.</param>
    public WeatherDataRow(
        DateTime time,
        WeatherCharacteristics weatherCharacteristics,
        int temperature,
        int windDirectionAzimuth,
        decimal windSpeed,
        int atmosphericPressure,
        int humidity)
    {
        this.Time = time;
        this.WeatherCharacteristics = weatherCharacteristics;
        this.Temperature = temperature;
        this.WindDirectionAzimuth = windDirectionAzimuth;
        this.WindSpeed = windSpeed;
        this.AtmosphericPressure = atmosphericPressure;
        this.Humidity = humidity;
    }

    /// <summary>
    /// Gets the date and time.
    /// </summary>
    public DateTime Time { get; }

    /// <summary>
    /// Gets the weather characteristics flags.
    /// </summary>
    public WeatherCharacteristics WeatherCharacteristics { get; }

    /// <summary>
    /// Gets the air temperature in degrees Celsius.
    /// </summary>
    public int Temperature { get; }

    /// <summary>
    /// Gets the wind direction azimuth angle (0..359 degrees).
    /// </summary>
    public int WindDirectionAzimuth { get; }

    /// <summary>
    /// Gets the wind speed in m/s.
    /// </summary>
    public decimal WindSpeed { get; }

    /// <summary>
    /// Gets the atmospheric pressure.
    /// </summary>
    public int AtmosphericPressure { get; }

    /// <summary>
    /// Gets the air humidity percentage.
    /// </summary>
    public int Humidity { get; }
}
