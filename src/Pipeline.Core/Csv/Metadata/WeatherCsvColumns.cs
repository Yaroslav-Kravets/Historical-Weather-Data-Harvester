// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Metadata;

/// <summary>
/// Provides constant names for the core columns that appear in normalized weather CSV files.
/// </summary>
public static class WeatherCsvColumns
{
    /// <summary>
    /// Format string for <see cref="DateTime"/> values in weather CSV files.
    /// </summary>
    public const string DateTimeFormat = WeatherScalarCsvColumns.DateTimeFormat;

    public const string DateTime = WeatherScalarCsvColumns.DateTime;
    public const string Temperature = WeatherScalarCsvColumns.Temperature;
    public const string WindDirection = WeatherScalarCsvColumns.WindDirection;
    public const string WindSpeed = WeatherScalarCsvColumns.WindSpeed;
    public const string AtmosphericPressure = WeatherScalarCsvColumns.AtmosphericPressure;
    public const string Humidity = WeatherScalarCsvColumns.Humidity;
    public const string WeatherCharacteristics = NormalizedWeatherCsvColumns.WeatherCharacteristics;

    public const string EnglishName = WeatherManifestCsvColumns.EnglishName;
    public const string NameInHtml = WeatherManifestCsvColumns.NameInHtml;

    public const string Place = WeatherManifestCsvColumns.Place;
    public const string Date = WeatherManifestCsvColumns.Date;
    public const string SourceFilePath = WeatherManifestCsvColumns.SourceFilePath;

    /// <summary>
    /// Column headers for manifest CSV files (<c>parsed-places.csv</c>, <c>weather-characteristics.csv</c>).
    /// </summary>
    public static readonly IReadOnlyList<string> ManifestColumns = WeatherManifestCsvColumns.ManifestColumns;

    /// <summary>
    /// Scalar measurement columns shared by normalized and denormalized weather CSV files.
    /// </summary>
    public static readonly IReadOnlyList<string> ScalarColumns = WeatherScalarCsvColumns.ScalarColumns;

    /// <summary>
    /// Ordered collection of all core columns in normalized weather CSV files.
    /// </summary>
    public static readonly IReadOnlyList<string> CoreColumns = NormalizedWeatherCsvColumns.CoreColumns;
}
