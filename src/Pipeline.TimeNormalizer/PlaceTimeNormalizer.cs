// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer;

using Common;
using Microsoft.Extensions.Logging;

public sealed class PlaceTimeNormalizer
{
    private readonly ILogger<PlaceTimeNormalizer> logger;
    private readonly ObservationTimeNormalizer observationTimeNormalizer;
    private readonly ObservationTimeInterpolator observationTimeInterpolator;
    private readonly ParsedSourceFilesManifestReader parsedSourceFilesManifestReader;

    public PlaceTimeNormalizer(
        ILogger<PlaceTimeNormalizer> logger,
        ObservationTimeNormalizer observationTimeNormalizer,
        ObservationTimeInterpolator observationTimeInterpolator,
        ParsedSourceFilesManifestReader parsedSourceFilesManifestReader)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(observationTimeNormalizer);
        Argument.ThrowIfNull(observationTimeInterpolator);
        Argument.ThrowIfNull(parsedSourceFilesManifestReader);

        this.logger = logger;
        this.observationTimeNormalizer = observationTimeNormalizer;
        this.observationTimeInterpolator = observationTimeInterpolator;
        this.parsedSourceFilesManifestReader = parsedSourceFilesManifestReader;
    }

    public PlaceTimeNormalizationResult NormalizePlace(
        string place,
        SortedDictionary<DateTime, List<WeatherDataRow>> dateEntries,
        IReadOnlyDictionary<string, Dictionary<DateTime, string>> sourceFilesByPlace,
        IReadOnlyList<TimeSpan> expectedObservationTimes,
        TimeNormalizationIssueCollector issueCollector)
    {
        Argument.ThrowIfNull(place);
        Argument.ThrowIfNull(dateEntries);
        Argument.ThrowIfNull(sourceFilesByPlace);
        Argument.ThrowIfNull(expectedObservationTimes);
        Argument.ThrowIfNull(issueCollector);
        var placeCounts = new PlaceTimeNormalizationCounts();
        var normalizedRows = new List<WeatherDataRow>();
        var normalizedDateEntries = new Dictionary<DateTime, IReadOnlyList<WeatherDataRow>>();
        var orderedDates = dateEntries.Keys.OrderBy(date => date).ToList();
        var missingTimeEntriesCount = 0;
        var successfulCount = 0;
        var unsuccessfulCount = 0;

        for (var index = 0; index < orderedDates.Count; index++)
        {
            var date = orderedDates[index];
            var currentDayRows = dateEntries[date];
            var sourceFilePath = this.parsedSourceFilesManifestReader.ResolveSourceFilePath(sourceFilesByPlace, place, date);

            IReadOnlyList<WeatherDataRow>? previousDayRows = null;
            if (index > 0)
            {
                var previousDate = orderedDates[index - 1];
                previousDayRows = normalizedDateEntries.TryGetValue(previousDate, out var normalizedPreviousRows)
                    ? normalizedPreviousRows
                    : dateEntries[previousDate];
            }

            IReadOnlyList<WeatherDataRow>? nextDayRows = null;
            if (index < orderedDates.Count - 1)
            {
                nextDayRows = dateEntries[orderedDates[index + 1]];
            }

            IReadOnlyList<WeatherDataRow>? normalizedDayRows = null;
            var missingTimeEntriesBefore = missingTimeEntriesCount;

            try
            {
                normalizedDayRows = this.observationTimeNormalizer.NormalizeOrThrow(
                    sourceFilePath,
                    currentDayRows,
                    expectedObservationTimes,
                    ref missingTimeEntriesCount);
            }
            catch (Exception timeNormalizationException)
            {
                var hadMissingTimeEntries = missingTimeEntriesCount > missingTimeEntriesBefore;

                if (this.observationTimeInterpolator.TryInterpolateMissingObservationTimes(
                        sourceFilePath,
                        date,
                        currentDayRows,
                        expectedObservationTimes,
                        previousDayRows,
                        nextDayRows,
                        out var interpolatedRows))
                {
                    normalizedDayRows = interpolatedRows;
                    this.logger.LogDebug(
                        "Normalized file {FilePath} using interpolation for missing observations.",
                        sourceFilePath);

                    if (hadMissingTimeEntries)
                    {
                        placeCounts.MissingTimeEntries++;
                        issueCollector.AddMissingTimeEntry(place);
                    }
                }
                else
                {
                    unsuccessfulCount++;
                    placeCounts.Unsuccessful++;
                    issueCollector.AddTimeNormalizationFailure(place);
                    this.logger.LogDebug(
                        timeNormalizationException,
                        "Unable to normalize HTML file {FilePath} even after interpolation attempt.",
                        sourceFilePath);
                    continue;
                }
            }

            normalizedDateEntries[date] = normalizedDayRows;
            normalizedRows.AddRange(normalizedDayRows);
            successfulCount++;
            placeCounts.Successful++;
        }

        return new PlaceTimeNormalizationResult(
            place,
            normalizedRows,
            placeCounts,
            normalizedDateEntries.Count,
            successfulCount,
            unsuccessfulCount,
            missingTimeEntriesCount);
    }
}
