// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Abstractions;
using Common;
using Microsoft.Extensions.Logging;

public sealed class TimeNormalizingPipeline
{
    private static readonly IReadOnlyList<TimeSpan> ExpectedObservationTimes = Enumerable.Range(0, 8)
        .Select(i => TimeSpan.FromHours(i * 3))
        .ToList();

    private readonly ILogger<TimeNormalizingPipeline> logger;
    private readonly IFileSystem fileSystem;
    private readonly PlaceCsvFileNameResolver placeCsvFileNameResolver;
    private readonly DenormalizedWeatherDataCsvReader denormalizedWeatherDataCsvReader;
    private readonly DenormalizedWeatherDataCsvWriter denormalizedWeatherDataCsvWriter;
    private readonly ParsedSourceFilesManifestReader parsedSourceFilesManifestReader;
    private readonly PlaceTimeNormalizer placeTimeNormalizer;
    private readonly NormalizedColumnsWeatherDataCsvWriter normalizedColumnsWeatherDataCsvWriter;
    private readonly TimeNormalizingReportWriter timeNormalizingReportWriter;

    public TimeNormalizingPipeline(
        ILogger<TimeNormalizingPipeline> logger,
        IFileSystem fileSystem,
        PlaceCsvFileNameResolver placeCsvFileNameResolver,
        DenormalizedWeatherDataCsvReader denormalizedWeatherDataCsvReader,
        DenormalizedWeatherDataCsvWriter denormalizedWeatherDataCsvWriter,
        ParsedSourceFilesManifestReader parsedSourceFilesManifestReader,
        PlaceTimeNormalizer placeTimeNormalizer,
        NormalizedColumnsWeatherDataCsvWriter normalizedColumnsWeatherDataCsvWriter,
        TimeNormalizingReportWriter timeNormalizingReportWriter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(placeCsvFileNameResolver);
        Argument.ThrowIfNull(denormalizedWeatherDataCsvReader);
        Argument.ThrowIfNull(denormalizedWeatherDataCsvWriter);
        Argument.ThrowIfNull(parsedSourceFilesManifestReader);
        Argument.ThrowIfNull(placeTimeNormalizer);
        Argument.ThrowIfNull(normalizedColumnsWeatherDataCsvWriter);
        Argument.ThrowIfNull(timeNormalizingReportWriter);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.placeCsvFileNameResolver = placeCsvFileNameResolver;
        this.denormalizedWeatherDataCsvReader = denormalizedWeatherDataCsvReader;
        this.denormalizedWeatherDataCsvWriter = denormalizedWeatherDataCsvWriter;
        this.parsedSourceFilesManifestReader = parsedSourceFilesManifestReader;
        this.placeTimeNormalizer = placeTimeNormalizer;
        this.normalizedColumnsWeatherDataCsvWriter = normalizedColumnsWeatherDataCsvWriter;
        this.timeNormalizingReportWriter = timeNormalizingReportWriter;
    }

    public void Run(TimeNormalizingRunOptions options)
    {
        Argument.ThrowIfNull(options);
        Argument.ThrowIfNull(options.HtmlWriter);
        if (options.RunInParallel)
        {
            this.logger.LogInformation(
                "Time normalizing stage start (parallel, max degree: {MaxDegree})",
                Environment.ProcessorCount);
        }
        else
        {
            this.logger.LogInformation("Time normalizing stage start (sequential)");
        }

        if (!this.fileSystem.Directory.Exists(options.ParsedStageDirectory))
        {
            throw new DirectoryNotFoundException($"Parsed stage directory not found: {options.ParsedStageDirectory}");
        }

        var denormalizedDataByPlace = this.denormalizedWeatherDataCsvReader.ReadAllPlaces(options.ParsedStageDirectory);
        if (denormalizedDataByPlace.Count == 0)
        {
            throw new InvalidOperationException(
                $"No denormalized place CSVs found in parsed stage directory '{options.ParsedStageDirectory}'.");
        }

        var sourceFilesByPlace = this.parsedSourceFilesManifestReader.ReadByPlaceAndDate(options.ParsedStageDirectory);
        var issueCollector = new TimeNormalizationIssueCollector();
        var placeResults = new ConcurrentDictionary<string, PlaceTimeNormalizationResult>(StringComparer.OrdinalIgnoreCase);
        var maxDegree = ParallelExecutionOptions.GetMaxDegreeOfParallelism(options.RunInParallel);
        var totalStopwatch = Stopwatch.StartNew();
        long totalPlaceProcessingTime = 0;

        Parallel.ForEach(
            denormalizedDataByPlace,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegree },
            kvp =>
            {
                var placeStopwatch = Stopwatch.StartNew();

                try
                {
                    var result = this.placeTimeNormalizer.NormalizePlace(
                        kvp.Key,
                        ToMutableDateEntries(kvp.Value),
                        sourceFilesByPlace,
                        ExpectedObservationTimes,
                        issueCollector);
                    placeResults[kvp.Key] = result;
                }
                finally
                {
                    placeStopwatch.Stop();
                    Interlocked.Add(ref totalPlaceProcessingTime, placeStopwatch.ElapsedMilliseconds);
                }
            });

        var normalizedRowsByPlace = new Dictionary<string, List<WeatherDataRow>>(StringComparer.OrdinalIgnoreCase);
        var normalizedFileCountsByPlace = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var timeNormalizationCountsByPlace = new Dictionary<string, PlaceTimeNormalizationCounts>(StringComparer.OrdinalIgnoreCase);
        var missingTimeEntriesCount = 0;
        var timeNormalizationSuccessfulCount = 0;
        var timeNormalizationUnsuccessfulCount = 0;

        foreach (var result in placeResults.Values.OrderBy(r => r.Place, StringComparer.OrdinalIgnoreCase))
        {
            normalizedRowsByPlace[result.Place] = result.NormalizedRows;
            normalizedFileCountsByPlace[result.Place] = result.NormalizedFileCount;
            timeNormalizationCountsByPlace[result.Place] = result.PlaceCounts;
            timeNormalizationSuccessfulCount += result.SuccessfulCount;
            timeNormalizationUnsuccessfulCount += result.UnsuccessfulCount;
            missingTimeEntriesCount += result.MissingTimeEntriesCount;
        }

        var normalizedColumnsDir = this.fileSystem.Path.Combine(options.TimeNormalizedStageDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);
        var projected = normalizedRowsByPlace.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<WeatherDataRow>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);
        this.normalizedColumnsWeatherDataCsvWriter.WritePlaceRows(projected, normalizedColumnsDir, "normalized");

        this.fileSystem.Directory.CreateDirectory(options.TimeNormalizedStageDirectory);

        foreach (var (placeName, rows) in normalizedRowsByPlace)
        {
            if (rows.Count == 0)
            {
                continue;
            }

            var csvFileName = this.placeCsvFileNameResolver.ToCsvFileName(placeName);
            this.denormalizedWeatherDataCsvWriter.WritePlaceRows(
                options.TimeNormalizedStageDirectory,
                csvFileName,
                rows.OrderBy(row => row.Time).ToList(),
                includePlaceColumn: true);
        }

        totalStopwatch.Stop();

        var totalPlaces = denormalizedDataByPlace.Count;
        var totalTime = totalStopwatch.Elapsed.TotalSeconds;
        var averageTime = totalPlaces > 0 ? (totalPlaceProcessingTime / (double)totalPlaces) / 1000.0 : 0;

        this.timeNormalizingReportWriter.WriteReport(
            options.HtmlWriter,
            totalPlaces,
            timeNormalizationSuccessfulCount,
            timeNormalizationUnsuccessfulCount,
            missingTimeEntriesCount,
            totalTime,
            averageTime,
            normalizedRowsByPlace,
            normalizedFileCountsByPlace,
            timeNormalizationCountsByPlace,
            issueCollector,
            options.ParsedStageDirectory);

        this.logger.LogInformation("Time normalizing stage complete");
    }

    private static SortedDictionary<DateTime, List<WeatherDataRow>> ToMutableDateEntries(
        IReadOnlyDictionary<DateTime, IReadOnlyList<WeatherDataRow>> dateEntries)
    {
        return new SortedDictionary<DateTime, List<WeatherDataRow>>(
            dateEntries.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToList()));
    }
}
