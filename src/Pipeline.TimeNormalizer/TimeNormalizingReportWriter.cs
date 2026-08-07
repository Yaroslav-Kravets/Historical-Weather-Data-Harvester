// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer;

using System.Globalization;
using System.IO.Abstractions;
using Common;
using HtmlLog;

public sealed class TimeNormalizingReportWriter
{
    private readonly TimeNormalizingPlaceErrorCountsBuilder errorCountsBuilder;
    private readonly DenormalizedWeatherDataCsvReader denormalizedWeatherDataCsvReader;
    private readonly PlaceCsvFileNameResolver placeCsvFileNameResolver;
    private readonly IFileSystem fileSystem;

    public TimeNormalizingReportWriter(
        TimeNormalizingPlaceErrorCountsBuilder errorCountsBuilder,
        DenormalizedWeatherDataCsvReader denormalizedWeatherDataCsvReader,
        PlaceCsvFileNameResolver placeCsvFileNameResolver,
        IFileSystem fileSystem)
    {
        Argument.ThrowIfNull(errorCountsBuilder);
        Argument.ThrowIfNull(denormalizedWeatherDataCsvReader);
        Argument.ThrowIfNull(placeCsvFileNameResolver);
        Argument.ThrowIfNull(fileSystem);

        this.errorCountsBuilder = errorCountsBuilder;
        this.denormalizedWeatherDataCsvReader = denormalizedWeatherDataCsvReader;
        this.placeCsvFileNameResolver = placeCsvFileNameResolver;
        this.fileSystem = fileSystem;
    }

    public void WriteReport(
        HtmlLogWriter writer,
        int totalPlaces,
        int timeNormalizationSuccessfulCount,
        int timeNormalizationUnsuccessfulCount,
        int missingTimeEntriesCount,
        double totalTimeSeconds,
        double averageTimePerPlaceSeconds,
        Dictionary<string, List<WeatherDataRow>> normalizedRowsByPlace,
        Dictionary<string, int> normalizedFileCountsByPlace,
        IReadOnlyDictionary<string, PlaceTimeNormalizationCounts> timeNormalizationCountsByPlace,
        TimeNormalizationIssueCollector issueCollector,
        string parsedStageDirectory)
    {
        Argument.ThrowIfNull(writer);
        Argument.ThrowIfNull(normalizedRowsByPlace);
        Argument.ThrowIfNull(normalizedFileCountsByPlace);
        Argument.ThrowIfNull(timeNormalizationCountsByPlace);
        Argument.ThrowIfNull(issueCollector);
        Argument.ThrowIfNull(parsedStageDirectory);
        WriteTimeNormalizationStatisticsTable(
            writer,
            totalPlaces,
            timeNormalizationSuccessfulCount,
            timeNormalizationUnsuccessfulCount,
            missingTimeEntriesCount,
            totalTimeSeconds,
            averageTimePerPlaceSeconds);
        WriteErrorsPerPlaceTable(
            writer,
            this.errorCountsBuilder.Build(
                normalizedRowsByPlace.Keys,
                issueCollector,
                timeNormalizationCountsByPlace));
        this.WriteNormalizedDataByPlaceTable(
            writer,
            normalizedRowsByPlace,
            normalizedFileCountsByPlace,
            timeNormalizationCountsByPlace);
        WriteRowCountComparisonTable(writer, this.BuildRowCountComparisons(parsedStageDirectory, normalizedRowsByPlace));
    }

    private static void WriteTimeNormalizationStatisticsTable(
        HtmlLogWriter writer,
        int totalPlaces,
        int timeNormalizationSuccessfulCount,
        int timeNormalizationUnsuccessfulCount,
        int missingTimeEntriesCount,
        double totalTimeSeconds,
        double averageTimePerPlaceSeconds)
    {
        var tableData = new[]
        {
            new { Metric = "Total places processed", Value = totalPlaces.ToString(CultureInfo.InvariantCulture) },
            new { Metric = "Time normalization successful", Value = timeNormalizationSuccessfulCount.ToString(CultureInfo.InvariantCulture) },
            new { Metric = "Time normalization unsuccessful", Value = timeNormalizationUnsuccessfulCount.ToString(CultureInfo.InvariantCulture) },
            new { Metric = "Files missing expected observation times", Value = missingTimeEntriesCount.ToString(CultureInfo.InvariantCulture) },
            new { Metric = "Total normalizing time", Value = DurationFormatter.FormatSeconds(totalTimeSeconds) },
            new { Metric = "Average time per place", Value = DurationFormatter.FormatSeconds(averageTimePerPlaceSeconds) },
        };

        writer.WriteTable(tableData, "Time Normalization Statistics");
    }

    private static void WriteErrorsPerPlaceTable(HtmlLogWriter writer, IReadOnlyList<TimeNormalizingPlaceErrorCounts> errorCountsByPlace)
    {
        var tableData = errorCountsByPlace
            .Select(counts => new
            {
                counts.Place,
                counts.MissingTimeEntries,
                counts.TimeNormalizationFailures,
                counts.TotalIssues,
            })
            .ToList();

        writer.WriteTable(tableData, "Errors Per Place");
    }

    private static void WriteRowCountComparisonTable(HtmlLogWriter writer, IReadOnlyList<RowCountComparisonStats> comparisons)
    {
        var tableData = comparisons
            .Select(stats => new
            {
                stats.Place,
                DenormalizedInputRows = stats.DenormalizedInputRows,
                NormalizedRows = stats.NormalizedRows,
                Delta = stats.Delta,
                DeltaPercent = stats.DeltaPercent.ToString("F2", CultureInfo.InvariantCulture) + "%",
            })
            .ToList();

        writer.WriteTable(tableData, "Row Count Comparison by Place");
    }

    private List<NormalizedPlaceDataStats> BuildPlaceDataStats(
        Dictionary<string, List<WeatherDataRow>> normalizedRowsByPlace,
        Dictionary<string, int> normalizedFileCountsByPlace)
    {
        return normalizedRowsByPlace
            .Select(pair =>
            {
                var place = pair.Key;
                var rows = pair.Value;
                var dates = rows.Select(row => row.Time.Date).Distinct().OrderBy(date => date).ToList();
                normalizedFileCountsByPlace.TryGetValue(place, out var normalizedFiles);

                return new NormalizedPlaceDataStats(
                    place,
                    normalizedFiles,
                    rows.Count,
                    dates.Count,
                    dates.Count > 0 ? dates.Min().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
                    dates.Count > 0 ? dates.Max().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null);
            })
            .OrderBy(stats => stats.Place, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<RowCountComparisonStats> BuildRowCountComparisons(
        string parsedStageDirectory,
        Dictionary<string, List<WeatherDataRow>> normalizedRowsByPlace)
    {
        var comparisons = new List<RowCountComparisonStats>();

        foreach (var (place, normalizedRows) in normalizedRowsByPlace.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var inputCsvPath = this.fileSystem.Path.Combine(parsedStageDirectory, this.placeCsvFileNameResolver.ToCsvFileName(place));
            var inputRows = this.denormalizedWeatherDataCsvReader.CountDataRows(inputCsvPath);
            var normalizedRowCount = normalizedRows.Count;
            var delta = normalizedRowCount - inputRows;
            var deltaPercent = inputRows > 0 ? (delta / (double)inputRows) * 100.0 : 0.0;

            comparisons.Add(new RowCountComparisonStats(place, inputRows, normalizedRowCount, delta, deltaPercent));
        }

        if (this.fileSystem.Directory.Exists(parsedStageDirectory))
        {
            foreach (var csvPath in CsvDirectoryFiles.EnumerateCsvFiles(this.fileSystem, parsedStageDirectory)
                         .Where(path => !WeatherCsvOutputPaths.IsStageRootSidecarCsvFileName(this.fileSystem.Path.GetFileName(path))))
            {
                var place = this.fileSystem.Path.GetFileNameWithoutExtension(csvPath);
                if (place is null || normalizedRowsByPlace.ContainsKey(place))
                {
                    continue;
                }

                var inputCsvPath = this.fileSystem.Path.Combine(parsedStageDirectory, this.placeCsvFileNameResolver.ToCsvFileName(place));
                var inputRows = this.denormalizedWeatherDataCsvReader.CountDataRows(inputCsvPath);
                comparisons.Add(new RowCountComparisonStats(place, inputRows, 0, -inputRows, inputRows > 0 ? -100.0 : 0.0));
            }
        }

        return comparisons;
    }

    private void WriteNormalizedDataByPlaceTable(
        HtmlLogWriter writer,
        Dictionary<string, List<WeatherDataRow>> normalizedRowsByPlace,
        Dictionary<string, int> normalizedFileCountsByPlace,
        IReadOnlyDictionary<string, PlaceTimeNormalizationCounts> timeNormalizationCountsByPlace)
    {
        var tableData = this.BuildPlaceDataStats(normalizedRowsByPlace, normalizedFileCountsByPlace)
            .Select(stats =>
            {
                timeNormalizationCountsByPlace.TryGetValue(stats.Place, out var placeCounts);

                return new
                {
                    stats.Place,
                    TimeNormalizationSuccessful = placeCounts?.Successful ?? 0,
                    TimeNormalizationUnsuccessful = placeCounts?.Unsuccessful ?? 0,
                    MissingTimeEntries = placeCounts?.MissingTimeEntries ?? 0,
                    stats.NormalizedFiles,
                    stats.DataRows,
                    stats.UniqueDates,
                    stats.FirstDate,
                    stats.LastDate,
                };
            })
            .ToList();

        writer.WriteTable(tableData, "Normalized Data by Place");
    }

    private sealed record NormalizedPlaceDataStats(
        string Place,
        int NormalizedFiles,
        int DataRows,
        int UniqueDates,
        string? FirstDate,
        string? LastDate);

    private sealed record RowCountComparisonStats(
        string Place,
        int DenormalizedInputRows,
        int NormalizedRows,
        int Delta,
        double DeltaPercent);
}
