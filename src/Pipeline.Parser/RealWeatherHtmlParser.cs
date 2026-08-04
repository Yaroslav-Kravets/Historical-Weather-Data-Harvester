// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using Common;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

public sealed class RealWeatherHtmlParser
{
    /// <summary>
    /// Compiled regex pattern to extract city name from title.
    /// Pattern matches "Архив погоды в [CityName]" where CityName is in Cyrillic characters.
    /// Handles city names that may contain hyphens (e.g., Ивано-Франковск) or consist of a single word.
    /// </summary>
    private static readonly Regex CityNameRegex = new Regex(
        @"Архив\s+погоды\s+в\s+(.*?)\.\s+Погода",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Compiled regex pattern to extract date from title.
    /// Pattern matches "за [Day] [Month] [Year] года" where Month is the HTML month name.
    /// Example: "за 10 декабрь 2016 года".
    /// </summary>
    private static readonly Regex DateRegex = new Regex(
        @"за\s+(\d+)\s+(январь|февраль|март|апрель|май|июнь|июль|август|сентябрь|октябрь|ноябрь|декабрь)\s+(\d{4})\s+года",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// HTML month names mapped to month numbers (1-12).
    /// </summary>
    private static readonly Dictionary<string, int> HtmlMonthNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "январь", 1 },
        { "февраль", 2 },
        { "март", 3 },
        { "апрель", 4 },
        { "май", 5 },
        { "июнь", 6 },
        { "июль", 7 },
        { "август", 8 },
        { "сентябрь", 9 },
        { "октябрь", 10 },
        { "ноябрь", 11 },
        { "декабрь", 12 },
    };

    private readonly IFileSystem fileSystem;
    private readonly ILogger<RealWeatherHtmlParser> logger;
    private readonly WeatherCharacteristicConverter weatherCharacteristicConverter;

    static RealWeatherHtmlParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public RealWeatherHtmlParser(
        IFileSystem fileSystem,
        ILogger<RealWeatherHtmlParser> logger,
        WeatherCharacteristicConverter weatherCharacteristicConverter)
    {
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(weatherCharacteristicConverter);

        this.fileSystem = fileSystem;
        this.logger = logger;
        this.weatherCharacteristicConverter = weatherCharacteristicConverter;
    }

    /// <summary>
    /// Loads and parses an HTML file, checking for syntax errors.
    /// Extracts the city name from the title element (title handling is encapsulated internally).
    /// </summary>
    /// <param name="filePath">The path to the HTML file to parse.</param>
    /// <returns>An HtmlParseResult containing the extracted city name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the HTML file is malformed or title is missing.</exception>
    public HtmlParseResult ParseFile(string filePath)
    {
        Argument.ThrowIfNull(filePath);

        using var fileStream = this.fileSystem.File.OpenRead(filePath);
        return this.Parse(fileStream, filePath);
    }

    public HtmlParseResult Parse(Stream stream, string logicalPath)
    {
        Argument.ThrowIfNull(stream);
        Argument.ThrowIfNull(logicalPath);

        // Load and parse the HTML (lenient parsing for not fully correct HTML)
        var doc = new HtmlDocument();
        doc.OptionCheckSyntax = false;           // Do not treat parse issues as hard errors
        doc.OptionFixNestedTags = true;         // Fix improperly nested tags
        doc.OptionAutoCloseOnEnd = true;        // Auto-close unclosed tags at end of document
        this.LoadDocument(doc, stream);

        if (doc.ParseErrors != null && doc.ParseErrors.Count() > 0)
        {
            var errors = string.Join("; ", doc.ParseErrors.Select(e => $"{e.Code}: {e.Reason} (Line {e.Line}, Column {e.LinePosition})"));
            this.logger.LogWarning(
                "HTML parse issues in '{FilePath}' (continuing with best-effort parse): {Errors}",
                logicalPath,
                errors);
        }

        // Extract city name from title using XPath and compiled regex
        var cityName = this.ExtractSingleMatchByXPathAndRegex(
            doc,
            "/html/head/title",
            CityNameRegex,
            logicalPath);

        this.logger.LogTrace("  Extracted city: {CityName}", cityName);

        // Extract date from title - get title text and apply date regex
        var titleElement = doc.DocumentNode.SelectSingleNode("/html/head/title");
        if (titleElement == null)
        {
            throw new InvalidOperationException($"Title element not found in file '{logicalPath}'");
        }

        var titleText = titleElement.InnerText?.Trim();
        if (string.IsNullOrWhiteSpace(titleText))
        {
            throw new InvalidOperationException($"Title element has empty or null text in file '{logicalPath}'");
        }

        // Parse date to YYYY-MM-dd format
        var parsedDate = this.ParseHtmlDateToYYYYMMDD(titleText, logicalPath);

        // Extract date from filename
        var dateFromFilename = this.ExtractDateFromFilename(logicalPath);

        // Self-test: compare dates
        if (parsedDate != dateFromFilename)
        {
            throw new InvalidOperationException(
                $"Date mismatch in file '{logicalPath}': Date from title '{parsedDate}' does not match date from filename '{dateFromFilename}'");
        }

        this.logger.LogTrace("  Extracted date: {Date} (validated against filename)", parsedDate);

        // Extract weather data rows from the archive table
        var date = DateTime.ParseExact(parsedDate, "yyyy-MM-dd", null);
        var weatherDataRows = this.ParseWeatherDataTable(doc, logicalPath, date);

        this.logger.LogTrace("  Extracted {RowCount} weather data rows", weatherDataRows.Count);

        return new HtmlParseResult(cityName, parsedDate, weatherDataRows);
    }

    private static void LoadDocumentFromStream(HtmlDocument doc, Stream stream)
    {
        var encoding = doc.DetectEncoding(stream, checkHtml: true);
        stream.Position = 0;

        if (encoding is null)
        {
            doc.Load(stream, detectEncodingFromByteOrderMarks: true);
            return;
        }

        doc.Load(stream, encoding);
    }

    private void LoadDocument(HtmlDocument doc, Stream stream)
    {
        // Production disk OpenRead streams are seekable; this branch avoids an extra buffer copy.
        if (stream.CanSeek)
        {
            LoadDocumentFromStream(doc, stream);
            return;
        }

        // Non-seekable streams (archive entries, some MockFileSystem paths) need a seekable copy for DetectEncoding.
        using var bufferedStream = new MemoryStream();
        stream.CopyTo(bufferedStream);
        bufferedStream.Position = 0;
        LoadDocumentFromStream(doc, bufferedStream);
    }

    /// <summary>
    /// Parses the weather data table from the HTML document.
    /// </summary>
    /// <param name="doc">The HTML document.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <param name="date">The date to combine with time values.</param>
    /// <returns>List of weather data rows from the archive table.</returns>
    private List<WeatherDataRow> ParseWeatherDataTable(HtmlDocument doc, string filePath, DateTime date)
    {
        var rows = new List<WeatherDataRow>();

        // Find the archive table with headers: Время, Характеристики погоды, Температура воздуха, Ветер м/с, Атм. дав., Влажность воздуха %
        // The table has class "archive_table table"
        var tableXPath = "//table[@class='archive_table table']";
        var table = doc.DocumentNode.SelectSingleNode(tableXPath);

        if (table == null)
        {
            throw new InvalidOperationException(
                $"Archive table not found by XPath '{tableXPath}' in file '{filePath}'");
        }

        // Get all data rows (skip the header row)
        var dataRows = table.SelectNodes(".//tr[td[@class='at_l at_time']]");

        if (dataRows == null || dataRows.Count == 0)
        {
            this.logger.LogWarning("No data rows found in archive table in file '{FilePath}'", filePath);
            return rows;
        }

        foreach (var row in dataRows)
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 6)
            {
                this.logger.LogWarning(
                    "Invalid row structure in archive table in file '{FilePath}'. Expected 6 columns, found {ColumnCount}",
                    filePath,
                    cells?.Count ?? 0);
                continue;
            }

            // Extract time (column 1) - format: "HH:mm" and combine with date
            var time = this.ParseTime(cells[0], filePath, date);

            // Extract weather characteristics (column 2) - text from ov_hide div
            var weatherCharacteristics = this.ExtractWeatherCharacteristics(cells[1], filePath);

            // Extract temperature (column 3) - format: "+5°C" or "-5°C"
            var temperature = this.ParseTemperature(cells[2], filePath);

            // Extract wind direction and speed (column 4)
            var (windDirectionAzimuth, windSpeed) = this.ExtractWindDirectionAndSpeed(cells[3], filePath);

            // Extract atmospheric pressure (column 5) - format: "740"
            var atmosphericPressure = this.ParseAtmosphericPressure(cells[4], filePath);

            // Extract humidity (column 6) - format: "93"
            var humidity = this.ParseHumidity(cells[5], filePath);

            rows.Add(new WeatherDataRow(
                time,
                weatherCharacteristics,
                temperature,
                windDirectionAzimuth,
                windSpeed,
                atmosphericPressure,
                humidity));
        }

        return rows;
    }

    /// <summary>
    /// Extracts text content from a table cell, handling nested divs and spans.
    /// </summary>
    /// <param name="cell">The table cell node.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <param name="fieldName">The field name for error reporting.</param>
    /// <returns>The extracted text, never null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the cell is null or the text cannot be extracted.</exception>
    private string ExtractTextFromCell(HtmlNode cell, string filePath, string fieldName)
    {
        if (cell == null)
        {
            throw new InvalidOperationException(
                $"Cell is null when extracting {fieldName} in file '{filePath}'");
        }

        // Get all text nodes, excluding hidden elements (ov_hide class)
        var textNodes = cell.SelectNodes(".//text()[not(ancestor::*[contains(@class, 'ov_hide')])]");

        if (textNodes == null || textNodes.Count == 0)
        {
            throw new InvalidOperationException(
                $"No text nodes found when extracting {fieldName} in file '{filePath}'");
        }

        var text = string.Join(" ", textNodes.Select(n => n.InnerText?.Trim()).Where(t => !string.IsNullOrWhiteSpace(t))).Trim();

        // Clean up multiple spaces
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                $"Empty or whitespace text found when extracting {fieldName} in file '{filePath}'");
        }

        return text;
    }

    /// <summary>
    /// Extracts weather characteristics from a cell, specifically from the ov_hide div.
    /// </summary>
    /// <param name="cell">The table cell node.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <returns>The extracted weather characteristics, never null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the cell is null or weather characteristics cannot be extracted.</exception>
    private WeatherCharacteristics ExtractWeatherCharacteristics(HtmlNode cell, string filePath)
    {
        if (cell == null)
        {
            throw new InvalidOperationException(
                $"Cell is null when extracting weather characteristics in file '{filePath}'");
        }

        // Find the div with class "ov_hide" which contains the weather description
        var weatherDiv = cell.SelectSingleNode(".//div[contains(@class, 'ov_hide')]");

        if (weatherDiv == null)
        {
            throw new InvalidOperationException(
                $"Weather characteristics div (ov_hide) not found in file '{filePath}'");
        }

        var text = weatherDiv.InnerText;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                $"Empty or whitespace weather characteristics found in file '{filePath}'");
        }

        var normalizedValues = text
            .ToLowerInvariant()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        if (normalizedValues.Count == 0)
        {
            throw new InvalidOperationException(
                $"No weather characteristics extracted from text '{text}' in file '{filePath}'");
        }

        return this.weatherCharacteristicConverter.FromStrings(normalizedValues, filePath);
    }

    /// <summary>
    /// Extracts wind direction and speed from a cell.
    /// </summary>
    /// <param name="cell">The table cell node.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <returns>A tuple containing wind direction and speed.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the cell is null or wind information cannot be extracted.</exception>
    private (int WindDirectionAzimuth, decimal WindSpeed) ExtractWindDirectionAndSpeed(HtmlNode cell, string filePath)
    {
        if (cell == null)
        {
            throw new InvalidOperationException(
                $"Cell is null when extracting wind information in file '{filePath}'");
        }

        // Extract wind direction from image alt or title attribute
        string? direction = null;
        var img = cell.SelectSingleNode(".//img");
        if (img != null)
        {
            var alt = img.GetAttributeValue("alt", string.Empty);
            var title = img.GetAttributeValue("title", string.Empty);
            direction = !string.IsNullOrWhiteSpace(alt) ? alt : (!string.IsNullOrWhiteSpace(title) ? title : null);
        }

        if (string.IsNullOrWhiteSpace(direction))
        {
            throw new InvalidOperationException(
                $"Wind direction not found (missing img alt/title) in file '{filePath}'");
        }

        // Convert textual wind direction to azimuth degrees
        var azimuth = WindDirectionAzimuthConverter.FromString(direction, filePath);

        // Extract wind speed from text nodes
        var textNodes = cell.SelectNodes(".//text()[not(ancestor::img)]");
        var speedText = textNodes != null
            ? string.Join(" ", textNodes.Select(n => n.InnerText?.Trim()).Where(t => !string.IsNullOrWhiteSpace(t))).Trim()
            : null;

        // Clean up speed text (remove extra spaces)
        if (!string.IsNullOrWhiteSpace(speedText))
        {
            speedText = System.Text.RegularExpressions.Regex.Replace(speedText, @"\s+", " ");
        }

        if (string.IsNullOrWhiteSpace(speedText))
        {
            throw new InvalidOperationException(
                $"Wind speed not found in file '{filePath}'");
        }

        // Parse wind speed to decimal
        if (!decimal.TryParse(speedText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var windSpeed))
        {
            throw new InvalidOperationException(
                $"Failed to parse wind speed '{speedText}' as decimal in file '{filePath}'");
        }

        return (azimuth, windSpeed);
    }

    /// <summary>
    /// Parses time from a cell and combines it with the date.
    /// </summary>
    /// <param name="cell">The table cell node.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <param name="date">The date to combine with the time.</param>
    /// <returns>The combined date and time.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the time cannot be extracted or parsed.</exception>
    private DateTime ParseTime(HtmlNode cell, string filePath, DateTime date)
    {
        var timeText = this.ExtractTextFromCell(cell, filePath, "time");

        // Parse time in format "HH:mm"
        if (!DateTime.TryParseExact(timeText, "HH:mm", null, System.Globalization.DateTimeStyles.None, out var timeOnly))
        {
            throw new InvalidOperationException(
                $"Failed to parse time '{timeText}' in format 'HH:mm' in file '{filePath}'");
        }

        // Combine date and time
        return date.Date.Add(timeOnly.TimeOfDay);
    }

    /// <summary>
    /// Parses temperature from a cell (e.g., "+5°C" or "-5°C").
    /// </summary>
    /// <param name="cell">The table cell node.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <returns>The temperature as an integer in degrees Celsius.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the temperature cannot be extracted or parsed.</exception>
    private int ParseTemperature(HtmlNode cell, string filePath)
    {
        var tempText = this.ExtractTextFromCell(cell, filePath, "temperature");

        // Decode HTML entities (e.g., &deg; -> °)
        tempText = System.Net.WebUtility.HtmlDecode(tempText);

        // Use regex to extract the numeric value with optional sign
        // Matches: +14, -5, 20, etc. (handles +, -, or no sign)
        var tempMatch = System.Text.RegularExpressions.Regex.Match(tempText, @"([+-]?\d+)");
        if (!tempMatch.Success)
        {
            throw new InvalidOperationException(
                $"Failed to extract numeric temperature from '{tempText}' in file '{filePath}'");
        }

        var cleanedText = tempMatch.Groups[1].Value.Trim();

        if (!int.TryParse(cleanedText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var temperature))
        {
            throw new InvalidOperationException(
                $"Failed to parse temperature '{tempText}' as integer in file '{filePath}'");
        }

        return temperature;
    }

    /// <summary>
    /// Parses atmospheric pressure from a cell (e.g., "740").
    /// </summary>
    /// <param name="cell">The table cell node.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <returns>The atmospheric pressure as an integer.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the atmospheric pressure cannot be extracted or parsed.</exception>
    private int ParseAtmosphericPressure(HtmlNode cell, string filePath)
    {
        var pressureText = this.ExtractTextFromCell(cell, filePath, "atmospheric pressure");

        // Remove any whitespace
        pressureText = pressureText.Trim();

        if (!int.TryParse(pressureText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var pressure))
        {
            throw new InvalidOperationException(
                $"Failed to parse atmospheric pressure '{pressureText}' as integer in file '{filePath}'");
        }

        return pressure;
    }

    /// <summary>
    /// Parses humidity from a cell (e.g., "93").
    /// </summary>
    /// <param name="cell">The table cell node.</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <returns>The humidity as an integer percentage.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the humidity cannot be extracted or parsed.</exception>
    private int ParseHumidity(HtmlNode cell, string filePath)
    {
        var humidityText = this.ExtractTextFromCell(cell, filePath, "humidity");

        // Remove any whitespace and % sign if present
        humidityText = humidityText.Replace("%", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

        if (!int.TryParse(humidityText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var humidity))
        {
            throw new InvalidOperationException(
                $"Failed to parse humidity '{humidityText}' as integer in file '{filePath}'");
        }

        return humidity;
    }

    /// <summary>
    /// Parses an HTML title date string (e.g., "10 декабрь 2016") to YYYY-MM-dd format.
    /// </summary>
    /// <param name="dateString">The date string in format "day month_name year" (e.g., "10 декабрь 2016").</param>
    /// <param name="filePath">The file path for error reporting.</param>
    /// <returns>Date in YYYY-MM-dd format (e.g., "2016-12-10").</returns>
    private string ParseHtmlDateToYYYYMMDD(string dateString, string filePath)
    {
        var match = DateRegex.Match(dateString);
        if (!match.Success || match.Groups.Count < 4)
        {
            throw new InvalidOperationException(
                $"Failed to parse date from string '{dateString}' in file '{filePath}'");
        }

        var day = match.Groups[1].Value;
        var monthName = match.Groups[2].Value;
        var year = match.Groups[3].Value;

        if (!HtmlMonthNames.TryGetValue(monthName, out var month))
        {
            throw new InvalidOperationException(
                $"Unknown HTML month name '{monthName}' in file '{filePath}'");
        }

        if (!int.TryParse(day, out var dayInt) || dayInt < 1 || dayInt > 31)
        {
            throw new InvalidOperationException(
                $"Invalid day value '{day}' in file '{filePath}'");
        }

        if (!int.TryParse(year, out var yearInt))
        {
            throw new InvalidOperationException(
                $"Invalid year value '{year}' in file '{filePath}'");
        }

        try
        {
            var date = new DateTime(yearInt, month, dayInt);
            return date.ToString("yyyy-MM-dd");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidOperationException(
                $"Invalid date '{day}-{month}-{year}' in file '{filePath}'", ex);
        }
    }

    /// <summary>
    /// Extracts date in YYYY-MM-dd format from filename.
    /// Expected filename format: "YYYY-M-D.html" or "YYYY-MM-DD.html" or "path/to/YYYY-M-D.html"
    /// Handles both single and double digit months and days.
    /// </summary>
    /// <param name="filePath">The full file path.</param>
    /// <returns>Date in YYYY-MM-dd format (e.g., "2016-12-10").</returns>
    private string ExtractDateFromFilename(string filePath)
    {
        var fileName = this.fileSystem.Path.GetFileName(filePath);

        // Match YYYY-M-D or YYYY-MM-DD format (handles single and double digits for month and day)
        var dateRegex = new Regex(@"(\d{4})-(\d{1,2})-(\d{1,2})", RegexOptions.Compiled);

        var match = dateRegex.Match(fileName);
        if (!match.Success || match.Groups.Count < 4)
        {
            throw new InvalidOperationException(
                $"Could not extract date from filename '{fileName}' in file path '{filePath}'. Expected format: YYYY-M-D.html or YYYY-MM-DD.html");
        }

        var year = match.Groups[1].Value;
        var month = match.Groups[2].Value;
        var day = match.Groups[3].Value;

        // Parse to ensure valid date and normalize to YYYY-MM-dd format
        var dateStr = $"{year}-{month.PadLeft(2, '0')}-{day.PadLeft(2, '0')}";

        // Validate the extracted date format
        if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            throw new InvalidOperationException(
                $"Invalid date format '{dateStr}' extracted from filename '{fileName}' in file path '{filePath}'");
        }

        return parsedDate.ToString("yyyy-MM-dd");
    }

    private string ExtractSingleMatchByXPathAndRegex(
        HtmlDocument doc,
        string xpath,
        Regex compiledRegex,
        string filePath)
    {
        // Check that exactly one element matches the XPath
        var elements = doc.DocumentNode.SelectNodes(xpath);

        if (elements == null || elements.Count == 0)
        {
            throw new InvalidOperationException($"Element not found by XPath '{xpath}' in file '{filePath}'");
        }

        if (elements.Count > 1)
        {
            throw new InvalidOperationException(
                $"Multiple elements ({elements.Count}) found by XPath '{xpath}' in file '{filePath}'. Expected exactly one element.");
        }

        // Get the single element
        var element = elements[0];

        // Extract and validate element text
        var text = element.InnerText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException($"Element found by XPath '{xpath}' has empty or null text in file '{filePath}'");
        }

        // Apply compiled regex pattern and validate matches
        var matches = compiledRegex.Matches(text);

        if (matches.Count == 0)
        {
            throw new InvalidOperationException(
                $"No regex match found for pattern '{compiledRegex}' in element text '{text}' (XPath: '{xpath}') in file '{filePath}'");
        }

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                $"Multiple regex matches found ({matches.Count}) for pattern '{compiledRegex}' in element text '{text}' (XPath: '{xpath}') in file '{filePath}'. Expected exactly one match.");
        }

        // Extract the first capture group value
        var match = matches[0];
        if (match.Groups.Count < 2)
        {
            throw new InvalidOperationException(
                $"Regex pattern '{compiledRegex}' does not contain a capture group in file '{filePath}'");
        }

        var capturedValue = match.Groups[1].Value;
        if (string.IsNullOrWhiteSpace(capturedValue))
        {
            throw new InvalidOperationException(
                $"Regex capture group returned empty value for pattern '{compiledRegex}' in file '{filePath}'");
        }

        return capturedValue;
    }
}
