// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLog;

using System.IO.Abstractions;
using System.Reflection;
using System.Text;
using Common;
using ScottPlot;

/// <summary>
/// Writes HTML processing summary reports with tables, images, and diagrams.
/// Supports writing tables from flat classes using reflection.
/// </summary>
public sealed class HtmlLogWriter : IDisposable
{
    /// <summary>
    /// Exact footer opening markup written by <see cref="HtmlLogFileHandler"/>; used to insert
    /// additional sections into a closed HTML report before the footer.
    /// </summary>
    private const string FooterStartMarker = "        <div class=\"footer\">";

    private static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private readonly HtmlLogFileManager fileManager;
    private readonly HtmlLogFileHandler fileHandler;
    private readonly string filePath;
    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlLogWriter"/> class.
    /// </summary>
    /// <param name="fileManager">The shared HTML log file manager.</param>
    /// <param name="filePath">The path to the HTML summary file.</param>
    /// <param name="title">The title of the HTML document.</param>
    public HtmlLogWriter(HtmlLogFileManager fileManager, string filePath, string title = "Log")
    {
        Argument.ThrowIfNull(fileManager);
        Argument.ThrowIfNull(filePath);
        Argument.ThrowIfNull(title);
        this.fileManager = fileManager;

        // Normalize once so relative/absolute equivalents share the same manager key.
        this.filePath = fileManager.FileSystem.Path.GetFullPath(filePath);
        this.fileHandler = this.fileManager.CreateHandler(this.filePath, title);
    }

    /// <summary>
    /// Renders a table as an HTML fragment (table-container div) without document chrome.
    /// </summary>
    /// <typeparam name="T">The type of objects in the collection.</typeparam>
    /// <returns>The table HTML fragment, or empty when <paramref name="items"/> is empty.</returns>
    public static string RenderTableHtml<T>(IEnumerable<T> items, string? tableTitle = null)
    {
        Argument.ThrowIfNull(items);

        var itemsList = items.ToList();
        if (itemsList.Count == 0)
        {
            return string.Empty;
        }

        var type = typeof(T);

        // Get all readable properties and fields
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !IsIndexedProperty(p))
            .ToList();

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .ToList();

        if (properties.Count == 0 && fields.Count == 0)
        {
            throw new InvalidOperationException($"Type {type.Name} has no public readable properties or fields.");
        }

        var builder = new System.Text.StringBuilder();
        var title = tableTitle ?? type.Name;
        builder.AppendLine($@"        <div class=""table-container"">
            <div class=""table-title"">{EscapeHtml(title)}</div>
            <table>");

        // Write table header
        builder.AppendLine("                <thead>");
        builder.Append("                    <tr>");

        // Always write row number column first
        builder.Append("<th class=\"numeric\">#</th>");

        foreach (var prop in properties)
        {
            var displayName = SplitPascalCase(prop.Name);
            var isNumeric = IsNumericType(prop.PropertyType);
            var classAttr = isNumeric ? " class=\"numeric\"" : string.Empty;
            builder.Append($"<th{classAttr}>{EscapeHtml(displayName)}</th>");
        }

        foreach (var field in fields)
        {
            var displayName = SplitPascalCase(field.Name);
            var isNumeric = IsNumericType(field.FieldType);
            var classAttr = isNumeric ? " class=\"numeric\"" : string.Empty;
            builder.Append($"<th{classAttr}>{EscapeHtml(displayName)}</th>");
        }

        builder.AppendLine("</tr>");
        builder.AppendLine("                </thead>");

        // Write table body
        builder.AppendLine("                <tbody>");

        for (int rowIndex = 0; rowIndex < itemsList.Count; rowIndex++)
        {
            var item = itemsList[rowIndex];
            builder.Append("                    <tr>");

            // Always write row number column first (1-based index)
            builder.Append($"<td class=\"numeric\">{rowIndex + 1}</td>");

            foreach (var prop in properties)
            {
                var value = GetPropertyValue(item, prop);
                var formattedValue = FormatValue(value);
                var cellContent = IsTrustedAnchorHtml(formattedValue) ? formattedValue : EscapeHtml(formattedValue);
                var isNumeric = IsNumericType(prop.PropertyType);
                var classAttr = isNumeric ? " class=\"numeric\"" : string.Empty;
                builder.Append($"<td{classAttr}>{cellContent}</td>");
            }

            foreach (var field in fields)
            {
                var value = field.GetValue(item);
                var formattedValue = FormatValue(value);
                var cellContent = IsTrustedAnchorHtml(formattedValue) ? formattedValue : EscapeHtml(formattedValue);
                var isNumeric = IsNumericType(field.FieldType);
                var classAttr = isNumeric ? " class=\"numeric\"" : string.Empty;
                builder.Append($"<td{classAttr}>{cellContent}</td>");
            }

            builder.AppendLine("</tr>");
        }

        builder.AppendLine("                </tbody>");
        builder.AppendLine("            </table>");
        builder.AppendLine("        </div>");
        return builder.ToString();
    }

    /// <summary>
    /// Inserts an HTML fragment into a closed report immediately before the footer.
    /// When <paramref name="replaceTableTitle"/> is set, any existing table-container with that
    /// title is removed first so repeated inserts stay idempotent.
    /// </summary>
    public static void InsertHtmlBeforeFooter(
        IFileSystem fileSystem,
        string htmlReportPath,
        string htmlFragment,
        string? replaceTableTitle = null)
    {
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(htmlReportPath);
        Argument.ThrowIfNull(htmlFragment);

        var existingHtml = fileSystem.File.ReadAllText(htmlReportPath, Utf8Bom);
        if (!string.IsNullOrEmpty(replaceTableTitle))
        {
            existingHtml = RemoveTableContainerByTitle(existingHtml, replaceTableTitle) ?? existingHtml;
        }

        var footerIndex = existingHtml.IndexOf(FooterStartMarker, StringComparison.Ordinal);
        if (footerIndex < 0)
        {
            throw new InvalidOperationException(
                $"HTML report '{htmlReportPath}' is missing the expected footer marker; cannot insert content.");
        }

        var updatedHtml = existingHtml.Insert(footerIndex, htmlFragment);
        fileSystem.File.WriteAllText(htmlReportPath, updatedHtml, Utf8Bom);
    }

    /// <summary>
    /// Writes a table from a collection of flat class objects.
    /// Uses property/field names as column headers and values as row data.
    /// </summary>
    /// <typeparam name="T">The type of objects in the collection.</typeparam>
    /// <param name="items">The collection of objects to write as a table.</param>
    /// <param name="tableTitle">Optional title for the table. If not provided, uses the type name.</param>
    public void WriteTable<T>(IEnumerable<T> items, string? tableTitle = null)
    {
        Argument.ThrowIfNull(items);
        if (this.isDisposed)
        {
            return;
        }

        var tableHtml = RenderTableHtml(items, tableTitle);
        if (tableHtml.Length == 0)
        {
            return;
        }

        this.fileHandler.Write(tableHtml);
    }

    /// <summary>
    /// Creates and writes a distribution diagram (histogram) using ScottPlot.
    /// </summary>
    /// <param name="data">The data values to plot in the distribution.</param>
    /// <param name="caption">Caption/subscription text for the diagram.</param>
    /// <param name="width">Width of the plot in pixels. Default is 800.</param>
    /// <param name="height">Height of the plot in pixels. Default is 400.</param>
    /// <param name="bins">Number of bins for the histogram. Default is 30.</param>
    public void WriteDistributionDiagram(
        IEnumerable<double> data,
        string? caption = null,
        int width = 800,
        int height = 400,
        int bins = 30)
    {
        Argument.ThrowIfNull(data);
        if (this.isDisposed)
        {
            return;
        }

        var dataArray = data.ToArray();
        if (dataArray.Length == 0)
        {
            return;
        }

        // Create the plot
        var plt = new Plot();

        // Calculate histogram bins
        var min = dataArray.Min();
        var max = dataArray.Max();
        var binWidth = (max - min) / bins;

        var binEdges = new double[bins + 1];
        var binCounts = new double[bins];

        for (int i = 0; i <= bins; i++)
        {
            binEdges[i] = min + (i * binWidth);
        }

        foreach (var value in dataArray)
        {
            var binIndex = (int)Math.Min((value - min) / binWidth, bins - 1);
            binCounts[binIndex]++;
        }

        // Create positions for bars (centers of bins)
        var positions = new double[bins];
        var values = new double[bins];
        for (int i = 0; i < bins; i++)
        {
            positions[i] = (binEdges[i] + binEdges[i + 1]) / 2;
            values[i] = binCounts[i];
        }

        // Add bars to create histogram
        var bars = plt.Add.Bars(positions, values);
        bars.Color = Colors.Blue;

        // Style the plot
        plt.Title(caption ?? "Distribution");
        plt.YLabel("Frequency");
        plt.XLabel("Value");

        var imageBytes = plt.GetImageBytes(width, height, ImageFormat.Png);
        var base64String = Convert.ToBase64String(imageBytes);
        var imgSrc = $"data:image/png;base64,{base64String}";

        this.fileHandler.WriteLine($@"        <div class=""image-container"">
            <img src=""{imgSrc}"" alt=""{EscapeHtml(caption ?? "Distribution Diagram")}"" />
            {(string.IsNullOrEmpty(caption) ? string.Empty : $@"<div class=""image-caption"">{EscapeHtml(caption)}</div>")}
        </div>");
    }

    /// <summary>
    /// Ensures the footer is written and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (!this.isDisposed)
        {
            // Footer is written by HtmlLogFileHandler.Dispose via RemoveHandler.
            this.fileManager.RemoveHandler(this.filePath);
            this.isDisposed = true;
        }
    }

    private static string? RemoveTableContainerByTitle(string html, string tableTitle)
    {
        var titleMarker = $@"<div class=""table-title"">{EscapeHtml(tableTitle)}</div>";
        var titleIndex = html.IndexOf(titleMarker, StringComparison.Ordinal);
        if (titleIndex < 0)
        {
            return null;
        }

        const string containerStart = @"        <div class=""table-container"">";
        var start = html.LastIndexOf(containerStart, titleIndex, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        var tableEnd = html.IndexOf("</table>", titleIndex, StringComparison.Ordinal);
        if (tableEnd < 0)
        {
            return null;
        }

        var closeDiv = html.IndexOf("</div>", tableEnd, StringComparison.Ordinal);
        if (closeDiv < 0)
        {
            return null;
        }

        var end = closeDiv + "</div>".Length;
        if (end < html.Length && html[end] == '\r')
        {
            end++;
        }

        if (end < html.Length && html[end] == '\n')
        {
            end++;
        }

        return html.Remove(start, end - start);
    }

    private static bool IsIndexedProperty(PropertyInfo property)
    {
        return property.GetIndexParameters().Length > 0;
    }

    private static object? GetPropertyValue(object? obj, PropertyInfo property)
    {
        if (obj is null)
        {
            return null;
        }

        try
        {
            return property.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        if (value is DateTimeOffset dto)
        {
            return dto.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
        }

        if (value is IFormattable formattable && !(value is string))
        {
            return formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
        }

        return value.ToString() ?? string.Empty;
    }

    private static string SplitPascalCase(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // Insert space before uppercase letters (except the first one)
        // Uses regex to match: lowercase or digit followed by uppercase
        return System.Text.RegularExpressions.Regex.Replace(
            text,
            @"(\p{Ll}|\d)(\p{Lu})",
            "$1 $2");
    }

    private static bool IsNumericType(Type type)
    {
        if (type == null)
        {
            return false;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(byte) ||
               underlyingType == typeof(uint) ||
               underlyingType == typeof(ulong) ||
               underlyingType == typeof(ushort) ||
               underlyingType == typeof(sbyte) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(decimal);
    }

    /// <summary>
    /// True only for a single trusted anchor cell (the shape report writers emit).
    /// Arbitrary tags such as <c>&lt;img&gt;</c> must go through <see cref="EscapeHtml"/>
    /// so untrusted paths cannot bypass escaping.
    /// </summary>
    private static bool IsTrustedAnchorHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        // Exactly one <a ...>...</a> with no nested tags in the link text.
        return System.Text.RegularExpressions.Regex.IsMatch(
            text,
            @"^<a\s[^>]*>[^<]*</a>$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }

    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
