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

public sealed class ObservationTimeNormalizer
{
    private readonly ILogger<ObservationTimeNormalizer> logger;

    public ObservationTimeNormalizer(ILogger<ObservationTimeNormalizer> logger)
    {
        Argument.ThrowIfNull(logger);

        this.logger = logger;
    }

    public IReadOnlyList<WeatherDataRow> NormalizeOrThrow(
        string sourceFilePath,
        IReadOnlyList<WeatherDataRow> dayRows,
        IReadOnlyCollection<TimeSpan> expectedObservationTimes,
        ref int missingTimeEntriesCount)
    {
        Argument.ThrowIfNull(sourceFilePath);
        Argument.ThrowIfNull(dayRows);
        Argument.ThrowIfNullOrEmpty(expectedObservationTimes);
        var rowsByTime = dayRows
            .GroupBy(row => row.Time.TimeOfDay)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Time).ToList());

        var normalizedRows = new List<WeatherDataRow>(expectedObservationTimes.Count);
        var missingTimes = new List<TimeSpan>();

        foreach (var expectedTime in expectedObservationTimes)
        {
            if (!rowsByTime.TryGetValue(expectedTime, out var rowsForTime) || rowsForTime.Count == 0)
            {
                missingTimes.Add(expectedTime);
                continue;
            }

            if (rowsForTime.Count > 1)
            {
                var duplicatesFormatted = string.Join(", ", rowsForTime.Select(r => r.Time.ToString("yyyy-MM-dd HH:mm")));
                this.logger.LogWarning(
                    "Multiple observations found for {ObservationTime} in file {FilePath}. Using the first occurrence. Entries: {Entries}",
                    expectedTime.ToString(@"hh\:mm"),
                    sourceFilePath,
                    duplicatesFormatted);
            }

            normalizedRows.Add(rowsForTime[0]);
        }

        if (missingTimes.Count > 0)
        {
            missingTimeEntriesCount++;
            var missingTimesFormatted = string.Join(", ", missingTimes.Select(ts => ts.ToString(@"hh\:mm")));
            throw new InvalidOperationException(
                $"Missing expected observation times ({missingTimesFormatted}) in file '{sourceFilePath}'.");
        }

        var extraObservationTimes = rowsByTime.Keys
            .Where(time => !expectedObservationTimes.Contains(time))
            .OrderBy(time => time)
            .Select(time => time.ToString(@"hh\:mm"))
            .ToList();

        if (extraObservationTimes.Count > 0)
        {
            this.logger.LogDebug(
                "Ignoring extra observation times {ExtraObservationTimes} in file {FilePath}",
                string.Join(", ", extraObservationTimes),
                sourceFilePath);
        }

        normalizedRows.Sort((left, right) => left.Time.CompareTo(right.Time));

        if (normalizedRows.Count != expectedObservationTimes.Count)
        {
            throw new InvalidOperationException(
                $"Unexpected row count after normalization. Expected {expectedObservationTimes.Count}, got {normalizedRows.Count} in file '{sourceFilePath}'.");
        }

        return normalizedRows;
    }
}
