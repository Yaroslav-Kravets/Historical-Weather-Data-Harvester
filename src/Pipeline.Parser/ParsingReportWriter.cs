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
using Common;
using HtmlLog;
using Microsoft.Extensions.Logging;

public sealed class ParsingReportWriter
{
    private readonly ILogger<ParsingReportWriter> logger;
    private readonly ParsingPlaceErrorCountsBuilder errorCountsBuilder;
    private readonly IFileSystem fileSystem;
    private readonly WeatherCharacteristicConverter weatherCharacteristicConverter;

    public ParsingReportWriter(
        ILogger<ParsingReportWriter> logger,
        ParsingPlaceErrorCountsBuilder errorCountsBuilder,
        IFileSystem fileSystem,
        WeatherCharacteristicConverter weatherCharacteristicConverter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(errorCountsBuilder);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(weatherCharacteristicConverter);

        this.logger = logger;
        this.errorCountsBuilder = errorCountsBuilder;
        this.fileSystem = fileSystem;
        this.weatherCharacteristicConverter = weatherCharacteristicConverter;
    }

    public void WriteReport(
        HtmlLogWriter writer,
        string sourcePath,
        bool isSevenZipSource,
        int totalFiles,
        int parsingSuccessfulCount,
        int parsingUnsuccessfulCount,
        double totalTimeSeconds,
        double averageTimePerFileSeconds,
        Dictionary<string, SortedDictionary<DateTime, ParsedDateEntry>> resultsByPlace,
        ParsingIssueCollector issueCollector,
        IReadOnlyList<ParsedFileInfo> flattenedParseResults)
    {
        Argument.ThrowIfNull(writer);
        Argument.ThrowIfNull(sourcePath);
        Argument.ThrowIfNull(resultsByPlace);
        Argument.ThrowIfNull(issueCollector);
        Argument.ThrowIfNull(flattenedParseResults);
        WriteParsingStatisticsTable(
            writer,
            totalFiles,
            parsingSuccessfulCount,
            parsingUnsuccessfulCount,
            totalTimeSeconds,
            averageTimePerFileSeconds,
            issueCollector);
        this.WritePerPlaceParsingSummaryTable(writer, resultsByPlace);
        WriteErrorsPerPlaceTable(writer, this.errorCountsBuilder.Build(resultsByPlace.Keys, issueCollector));
        WritePlacePathSelfCheckSummaryTable(writer, issueCollector);
        WritePlacePathSelfCheckTable(writer, issueCollector);
        this.WriteParsedDataByPlaceTable(writer, resultsByPlace);
        this.LogAllKnownWeatherCharacteristics(writer);
        this.LogAllKnownWindDirections(writer);
        WriteRowCountDistributionTable(writer, flattenedParseResults);

        var rowCounts = flattenedParseResults.Select(r => (double)r.Result.WeatherDataRows.Count).ToList();
        if (rowCounts.Count > 0)
        {
            writer.WriteDistributionDiagram(rowCounts, "Distribution of Row List Counts");
        }

        this.WriteTimesDistributionTable(writer, sourcePath, isSevenZipSource, flattenedParseResults);
    }

    private static void WriteParsingStatisticsTable(
        HtmlLogWriter writer,
        int totalFiles,
        int parsingSuccessfulCount,
        int parsingUnsuccessfulCount,
        double totalTimeSeconds,
        double averageTimePerFileSeconds,
        ParsingIssueCollector issueCollector)
    {
        var metrics = new List<(string Metric, string Value)>
        {
            ("Total files processed", totalFiles.ToString(CultureInfo.InvariantCulture)),
            ("Parsing successful", parsingSuccessfulCount.ToString(CultureInfo.InvariantCulture)),
            ("Parsing unsuccessful", parsingUnsuccessfulCount.ToString(CultureInfo.InvariantCulture)),
            ("Total parsing time", DurationFormatter.FormatSeconds(totalTimeSeconds)),
            ("Average time per file", DurationFormatter.FormatSeconds(averageTimePerFileSeconds)),
        };

        var pathSelfCheckTotals = issueCollector.GetPathSelfCheckTotals();
        if (pathSelfCheckTotals.FilesChecked > 0)
        {
            metrics.Add(("Path self-check files checked", pathSelfCheckTotals.FilesChecked.ToString(CultureInfo.InvariantCulture)));
            metrics.Add(("Path self-check matches", pathSelfCheckTotals.Matches.ToString(CultureInfo.InvariantCulture)));
            metrics.Add(("Path self-check mismatches", pathSelfCheckTotals.Mismatches.ToString(CultureInfo.InvariantCulture)));
        }

        writer.WriteTable(
            metrics.Select(metric => new { metric.Metric, metric.Value }),
            "Parsing Statistics");
    }

    private static void WritePlacePathSelfCheckSummaryTable(HtmlLogWriter writer, ParsingIssueCollector issueCollector)
    {
        var summaryByPlace = issueCollector.GetPathSelfCheckSummaryByPlace();
        if (summaryByPlace.Count == 0)
        {
            return;
        }

        var tableData = summaryByPlace
            .Select(summary => new
            {
                summary.Place,
                FilesChecked = summary.FilesChecked,
                summary.Matches,
                summary.Mismatches,
            })
            .ToList();

        writer.WriteTable(tableData, "Place Path Self-Check Summary");
    }

    private static void WritePlacePathSelfCheckTable(HtmlLogWriter writer, ParsingIssueCollector issueCollector)
    {
        var selfChecks = issueCollector.GetPathSelfChecks();
        if (selfChecks.Count == 0)
        {
            return;
        }

        var tableData = selfChecks
            .Select(entry => new
            {
                entry.FilePath,
                PathPlace = entry.PathPlaceDisplay,
                HtmlCityName = entry.HtmlCityName,
                HtmlPlace = entry.HtmlPlaceDisplay,
                Match = entry.IsMatch ? "Yes" : "No",
            })
            .ToList();

        writer.WriteTable(tableData, "Place Path Self-Check");
    }

    private static void WriteErrorsPerPlaceTable(HtmlLogWriter writer, IReadOnlyList<ParsingPlaceErrorCounts> errorCountsByPlace)
    {
        var tableData = errorCountsByPlace
            .Select(counts => new
            {
                counts.Place,
                counts.ParseFailures,
                counts.SkippedFiles,
                counts.DuplicateDates,
                counts.PathPlaceMismatches,
                counts.TotalIssues,
            })
            .ToList();

        writer.WriteTable(tableData, "Errors Per Place");
    }

    private static void WriteRowCountDistributionTable(HtmlLogWriter writer, IEnumerable<ParsedFileInfo> parseResults)
    {
        var tableData = parseResults
            .GroupBy(info => info.Result.WeatherDataRows.Count)
            .Select(g => new
            {
                RealWeatherDataRowsCount = g.Key,
                ParsedFilesCount = g.Count(),
            })
            .OrderBy(x => x.RealWeatherDataRowsCount)
            .ToList();

        writer.WriteTable(tableData, "Row List Count Distribution");
    }

    private List<PlaceDataStats> BuildPlaceDataStats(
        Dictionary<string, SortedDictionary<DateTime, ParsedDateEntry>> resultsByPlace)
    {
        return resultsByPlace
            .Select(pair =>
            {
                var place = pair.Key;
                var resultsByDate = pair.Value;
                var dates = resultsByDate.Keys.ToList();
                var dataRows = resultsByDate.Values.Sum(entry => entry.Result.WeatherDataRows.Count);

                return new PlaceDataStats(
                    place,
                    resultsByDate.Count,
                    dataRows,
                    dates.Count,
                    dates.Count > 0 ? dates.Min().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
                    dates.Count > 0 ? dates.Max().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null);
            })
            .OrderBy(stats => stats.Place, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void WritePerPlaceParsingSummaryTable(
        HtmlLogWriter writer,
        Dictionary<string, SortedDictionary<DateTime, ParsedDateEntry>> resultsByPlace)
    {
        var tableData = this.BuildPlaceDataStats(resultsByPlace)
            .Select(stats => new
            {
                stats.Place,
                ParsedFiles = stats.ParsedFiles,
                DataRows = stats.DataRows,
                UniqueDates = stats.UniqueDates,
                stats.FirstDate,
                stats.LastDate,
            })
            .ToList();

        writer.WriteTable(tableData, "Per-Place Parsing Summary");
    }

    private void WriteParsedDataByPlaceTable(
        HtmlLogWriter writer,
        Dictionary<string, SortedDictionary<DateTime, ParsedDateEntry>> resultsByPlace)
    {
        var tableData = this.BuildPlaceDataStats(resultsByPlace)
            .Select(stats => new
            {
                stats.Place,
                stats.DataRows,
                stats.UniqueDates,
                stats.FirstDate,
                stats.LastDate,
            })
            .ToList();

        writer.WriteTable(tableData, "Parsed Data by Place");
    }

    private void WriteTimesDistributionTable(
        HtmlLogWriter writer,
        string sourcePath,
        bool isSevenZipSource,
        IEnumerable<ParsedFileInfo> parseResults)
    {
        var tableData = parseResults
            .Select(info =>
            {
                var fileTimes = info.Result.WeatherDataRows
                    .Select(row => row.Time.TimeOfDay)
                    .OrderBy(t => t)
                    .ToList();
                var timesKey = string.Join(",", fileTimes.Select(t => t.ToString(@"hh\:mm")));
                return new { TimesKey = timesKey, Info = info, FileTimes = fileTimes };
            })
            .GroupBy(x => x.TimesKey)
            .Select(g =>
            {
                var infos = g.Select(x => x.Info).ToList();
                var timesList = g.First().FileTimes;
                var rowsCount = infos.First().Result.WeatherDataRows.Count;
                var parsedFilesCount = infos.Count;
                var dates = infos.Select(info => info.Date).ToList();

                var exampleLinks = infos
                    .Take(parsedFilesCount == 1 ? 1 : 2)
                    .Select(info => this.FormatExampleFileReference(sourcePath, isSevenZipSource, info.FilePath))
                    .ToList();

                return new
                {
                    Times = string.Join(", ", timesList.Select(t => t.ToString(@"hh\:mm"))),
                    RowsCount = rowsCount,
                    ParsedFilesCount = parsedFilesCount,
                    MinimumDate = dates.Any() ? dates.Min().ToString("yyyy-MM-dd") : (string?)null,
                    MaximumDate = dates.Any() ? dates.Max().ToString("yyyy-MM-dd") : (string?)null,
                    ExampleUris = string.Join(", ", exampleLinks),
                };
            })
            .OrderBy(x => x.MinimumDate)
            .ToList();

        writer.WriteTable(tableData, "Times Distribution");
    }

    private string FormatExampleFileReference(string sourcePath, bool isSevenZipSource, string filePath)
    {
        if (isSevenZipSource)
        {
            // Plain text: HtmlLogWriter.WriteTable escapes once via EscapeHtml.
            var archiveName = this.fileSystem.Path.GetFileName(sourcePath);
            return $"{archiveName}!{filePath}";
        }

        var fileName = this.fileSystem.Path.GetFileName(filePath);
        var fileUri = this.fileSystem.Path.IsPathRooted(filePath)
            ? new Uri(filePath).AbsoluteUri
            : new Uri(this.fileSystem.Path.GetFullPath(filePath)).AbsoluteUri;

        // WriteTable treats <a> as trusted markup (ContainsHtml); encode href + text.
        var encodedHref = System.Net.WebUtility.HtmlEncode(fileUri);
        var encodedFileName = System.Net.WebUtility.HtmlEncode(fileName);
        return $"<a href=\"{encodedHref}\" target=\"_blank\">{encodedFileName}</a>";
    }

    private void LogAllKnownWeatherCharacteristics(HtmlLogWriter htmlWriter)
    {
        var knownCharacteristics = this.weatherCharacteristicConverter.GetAllKnownCharacteristics();

        if (knownCharacteristics.Count == 0)
        {
            this.logger.LogWarning("No known weather characteristics are registered in the converter.");
            return;
        }

        this.logger.LogInformation("Total known weather characteristics: {WeatherCharacteristicsCount}", knownCharacteristics.Count);

        htmlWriter.WriteTable(
            knownCharacteristics.Select(value => new { Value = value }).ToList(),
            "Available Weather Characteristics");
    }

    private void LogAllKnownWindDirections(HtmlLogWriter htmlWriter)
    {
        var mappings = WindDirectionAzimuthConverter.GetAllKnownDirectionMappings();

        if (mappings.Count == 0)
        {
            this.logger.LogWarning("No known wind directions are registered in the converter.");
            return;
        }

        this.logger.LogInformation("Total known wind directions: {WindDirectionsCount}", mappings.Count);

        htmlWriter.WriteTable(
            mappings.Select(mapping => new { Value = mapping.Name, Degrees = mapping.Azimuth }).ToList(),
            "Available Wind Directions");
    }

    private sealed record PlaceDataStats(
        string Place,
        int ParsedFiles,
        int DataRows,
        int UniqueDates,
        string? FirstDate,
        string? LastDate);
}
