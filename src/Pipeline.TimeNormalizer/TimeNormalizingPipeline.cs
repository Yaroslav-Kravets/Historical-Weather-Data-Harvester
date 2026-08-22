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
using HtmlLog;
using Microsoft.Extensions.Logging;

public sealed class TimeNormalizingPipeline
{
    private static readonly IReadOnlyList<TimeSpan> ExpectedObservationTimes = Enumerable.Range(0, 8)
        .Select(i => TimeSpan.FromHours(i * 3))
        .ToList();

    private readonly ILogger<TimeNormalizingPipeline> logger;
    private readonly IFileSystem fileSystem;
    private readonly HtmlLogFileManager htmlLogFileManager;
    private readonly PlaceCsvFileNameResolver placeCsvFileNameResolver;
    private readonly WideFormatWeatherDataCsvReader wideFormatWeatherDataCsvReader;
    private readonly WideFormatWeatherDataCsvWriter wideFormatWeatherDataCsvWriter;
    private readonly ParsedSourceFilesManifestReader parsedSourceFilesManifestReader;
    private readonly PlaceTimeNormalizer placeTimeNormalizer;
    private readonly NarrowFormatWeatherDataCsvWriter narrowFormatWeatherDataCsvWriter;
    private readonly TimeNormalizingReportWriter timeNormalizingReportWriter;

    public TimeNormalizingPipeline(
        ILogger<TimeNormalizingPipeline> logger,
        IFileSystem fileSystem,
        HtmlLogFileManager htmlLogFileManager,
        PlaceCsvFileNameResolver placeCsvFileNameResolver,
        WideFormatWeatherDataCsvReader wideFormatWeatherDataCsvReader,
        WideFormatWeatherDataCsvWriter wideFormatWeatherDataCsvWriter,
        ParsedSourceFilesManifestReader parsedSourceFilesManifestReader,
        PlaceTimeNormalizer placeTimeNormalizer,
        NarrowFormatWeatherDataCsvWriter narrowFormatWeatherDataCsvWriter,
        TimeNormalizingReportWriter timeNormalizingReportWriter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(htmlLogFileManager);
        Argument.ThrowIfNull(placeCsvFileNameResolver);
        Argument.ThrowIfNull(wideFormatWeatherDataCsvReader);
        Argument.ThrowIfNull(wideFormatWeatherDataCsvWriter);
        Argument.ThrowIfNull(parsedSourceFilesManifestReader);
        Argument.ThrowIfNull(placeTimeNormalizer);
        Argument.ThrowIfNull(narrowFormatWeatherDataCsvWriter);
        Argument.ThrowIfNull(timeNormalizingReportWriter);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.htmlLogFileManager = htmlLogFileManager;
        this.placeCsvFileNameResolver = placeCsvFileNameResolver;
        this.wideFormatWeatherDataCsvReader = wideFormatWeatherDataCsvReader;
        this.wideFormatWeatherDataCsvWriter = wideFormatWeatherDataCsvWriter;
        this.parsedSourceFilesManifestReader = parsedSourceFilesManifestReader;
        this.placeTimeNormalizer = placeTimeNormalizer;
        this.narrowFormatWeatherDataCsvWriter = narrowFormatWeatherDataCsvWriter;
        this.timeNormalizingReportWriter = timeNormalizingReportWriter;
    }

    public void Run(TimeNormalizingRunOptions options)
    {
        Argument.ThrowIfNull(options);
        Argument.ThrowIfNull(options.HtmlReportPath);

        this.logger.LogInformation("Start");

        if (options.RunInParallel)
        {
            this.logger.LogInformation(
                "Time normalizing mode: parallel (max degree: {MaxDegree})",
                Environment.ProcessorCount);
        }
        else
        {
            this.logger.LogInformation("Time normalizing mode: sequential");
        }

        if (!this.fileSystem.Directory.Exists(options.ParsedStageDirectory))
        {
            throw new DirectoryNotFoundException($"Parsed stage directory not found: {options.ParsedStageDirectory}");
        }

        var parsedWideFormatDirectory = this.fileSystem.Path.Combine(
            options.ParsedStageDirectory,
            WeatherCsvOutputPaths.WideFormatDirectoryName);
        var wideFormatDataByPlace = this.wideFormatWeatherDataCsvReader.ReadAllPlaces(parsedWideFormatDirectory);
        if (wideFormatDataByPlace.Count == 0)
        {
            throw new InvalidOperationException(
                $"No wide-format place CSVs found in '{parsedWideFormatDirectory}'.");
        }

        var sourceFilesByPlace = this.parsedSourceFilesManifestReader.ReadByPlaceAndDate(options.ParsedStageDirectory);
        var issueCollector = new TimeNormalizationIssueCollector();
        var placeResults = new ConcurrentDictionary<string, PlaceTimeNormalizationResult>(StringComparer.OrdinalIgnoreCase);
        var maxDegree = ParallelExecutionOptions.GetMaxDegreeOfParallelism(options.RunInParallel);
        var totalStopwatch = Stopwatch.StartNew();
        long totalPlaceProcessingTime = 0;

        Parallel.ForEach(
            wideFormatDataByPlace,
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

        var narrowFormatDir = this.fileSystem.Path.Combine(
            options.TimeNormalizedStageDirectory,
            WeatherCsvOutputPaths.NarrowFormatDirectoryName);
        var wideFormatDir = this.fileSystem.Path.Combine(
            options.TimeNormalizedStageDirectory,
            WeatherCsvOutputPaths.WideFormatDirectoryName);
        this.fileSystem.Directory.CreateDirectory(options.TimeNormalizedStageDirectory);
        this.fileSystem.Directory.CreateDirectory(narrowFormatDir);
        this.fileSystem.Directory.CreateDirectory(wideFormatDir);

        var projected = normalizedRowsByPlace.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<WeatherDataRow>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);
        this.narrowFormatWeatherDataCsvWriter.WritePlaceRows(projected, narrowFormatDir, "normalized");

        foreach (var (placeName, rows) in normalizedRowsByPlace)
        {
            if (rows.Count == 0)
            {
                continue;
            }

            var csvFileName = this.placeCsvFileNameResolver.ToCsvFileName(placeName);
            this.wideFormatWeatherDataCsvWriter.WritePlaceRows(
                wideFormatDir,
                csvFileName,
                rows.OrderBy(row => row.Time).ToList(),
                includePlaceColumn: true);
        }

        totalStopwatch.Stop();

        var totalPlaces = wideFormatDataByPlace.Count;
        var totalTime = totalStopwatch.Elapsed.TotalSeconds;
        var averageTime = totalPlaces > 0 ? (totalPlaceProcessingTime / (double)totalPlaces) / 1000.0 : 0;

        this.timeNormalizingReportWriter.WriteReport(
            this.htmlLogFileManager,
            options.HtmlReportPath,
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
            parsedWideFormatDirectory);

        this.logger.LogInformation("Finish");
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
