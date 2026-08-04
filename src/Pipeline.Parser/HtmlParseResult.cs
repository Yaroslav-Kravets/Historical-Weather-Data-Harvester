// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

/// <summary>
/// Represents the result of parsing an HTML file.
/// </summary>
public sealed class HtmlParseResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlParseResult"/> class.
    /// </summary>
    /// <param name="cityName">The city name extracted from the title.</param>
    /// <param name="date">The date extracted from the title in YYYY-MM-dd format.</param>
    /// <param name="weatherDataRows">The list of weather data rows from the archive table.</param>
    public HtmlParseResult(string? cityName, string? date, List<WeatherDataRow>? weatherDataRows = null)
    {
        this.CityName = cityName;
        this.Date = date;
        this.WeatherDataRows = weatherDataRows ?? new List<WeatherDataRow>();
    }

    /// <summary>
    /// Gets the city name extracted from the title.
    /// </summary>
    public string? CityName { get; }

    /// <summary>
    /// Gets the date extracted from the title and validated against the filename.
    /// Format: YYYY-MM-dd (e.g., "2016-12-10").
    /// </summary>
    public string? Date { get; }

    /// <summary>
    /// Gets the list of weather data rows from the archive table.
    /// </summary>
    public List<WeatherDataRow> WeatherDataRows { get; }
}
