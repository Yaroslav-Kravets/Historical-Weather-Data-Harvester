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

public sealed class ObservationTimeInterpolator
{
    private readonly ILogger<ObservationTimeInterpolator> logger;

    public ObservationTimeInterpolator(ILogger<ObservationTimeInterpolator> logger)
    {
        Argument.ThrowIfNull(logger);

        this.logger = logger;
    }

    public bool TryInterpolateMissingObservationTimes(
        string sourceFilePath,
        DateTime date,
        IReadOnlyList<WeatherDataRow> dayRows,
        IReadOnlyCollection<TimeSpan> expectedObservationTimes,
        IReadOnlyList<WeatherDataRow>? previousDayRows,
        IReadOnlyList<WeatherDataRow>? nextDayRows,
        out IReadOnlyList<WeatherDataRow> interpolatedRows)
    {
        Argument.ThrowIfNull(sourceFilePath);
        Argument.ThrowIfNull(dayRows);
        Argument.ThrowIfNullOrEmpty(expectedObservationTimes);
        interpolatedRows = Array.Empty<WeatherDataRow>();

        if (dayRows.Count == 0)
        {
            return false;
        }

        var rowsGroupedByTime = dayRows
            .GroupBy(row => row.Time.TimeOfDay)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Time).First());

        if (rowsGroupedByTime.Count == 0)
        {
            return false;
        }

        var candidateRows = new List<WeatherDataRow>(rowsGroupedByTime.Count
            + (previousDayRows?.Count ?? 0)
            + (nextDayRows?.Count ?? 0));

        candidateRows.AddRange(rowsGroupedByTime.Values);

        if (previousDayRows != null)
        {
            candidateRows.AddRange(previousDayRows);
        }

        if (nextDayRows != null)
        {
            candidateRows.AddRange(nextDayRows);
        }

        candidateRows = candidateRows.OrderBy(row => row.Time).ToList();

        if (candidateRows.Count < 2)
        {
            return false;
        }

        var baseDate = date.Date;
        var sortedExpectedTimes = expectedObservationTimes.OrderBy(time => time).ToList();
        var normalizedRows = new List<WeatherDataRow>(sortedExpectedTimes.Count);

        foreach (var expectedTime in sortedExpectedTimes)
        {
            if (rowsGroupedByTime.TryGetValue(expectedTime, out var existingRow))
            {
                normalizedRows.Add(existingRow);
                continue;
            }

            if (!TryCreateInterpolatedRow(expectedTime, baseDate, candidateRows, out var interpolatedRow))
            {
                this.logger.LogDebug(
                    "Interpolation not possible for observation time {ObservationTime} in file {FilePath}",
                    expectedTime.ToString(@"hh\:mm"),
                    sourceFilePath);
                return false;
            }

            normalizedRows.Add(interpolatedRow);
        }

        normalizedRows.Sort((left, right) => left.Time.CompareTo(right.Time));
        interpolatedRows = normalizedRows;
        return true;
    }

    private static bool TryCreateInterpolatedRow(
        TimeSpan targetTime,
        DateTime baseDate,
        IReadOnlyList<WeatherDataRow> orderedCandidateRows,
        out WeatherDataRow interpolatedRow)
    {
        interpolatedRow = default!;

        var targetDateTime = baseDate.Add(targetTime);

        var previousRow = orderedCandidateRows.LastOrDefault(row => row.Time < targetDateTime);
        var nextRow = orderedCandidateRows.FirstOrDefault(row => row.Time > targetDateTime);

        if (previousRow == null || nextRow == null)
        {
            return false;
        }

        var totalMinutes = (nextRow.Time - previousRow.Time).TotalMinutes;
        if (totalMinutes <= 0)
        {
            return false;
        }

        var elapsedMinutes = (targetDateTime - previousRow.Time).TotalMinutes;
        if (elapsedMinutes < 0 || elapsedMinutes > totalMinutes)
        {
            return false;
        }

        var ratio = elapsedMinutes / totalMinutes;

        var temperature = InterpolateInt(previousRow.Temperature, nextRow.Temperature, ratio);
        var windSpeed = InterpolateDecimal(previousRow.WindSpeed, nextRow.WindSpeed, ratio);
        var atmosphericPressure = InterpolateInt(previousRow.AtmosphericPressure, nextRow.AtmosphericPressure, ratio);
        var humidity = InterpolateInt(previousRow.Humidity, nextRow.Humidity, ratio);

        var windDirectionAzimuth = ratio <= 0.5 ? previousRow.WindDirectionAzimuth : nextRow.WindDirectionAzimuth;
        var characteristics = previousRow.WeatherCharacteristics | nextRow.WeatherCharacteristics;

        interpolatedRow = new WeatherDataRow(
            targetDateTime,
            characteristics,
            temperature,
            windDirectionAzimuth,
            windSpeed,
            atmosphericPressure,
            humidity);

        return true;
    }

    private static int InterpolateInt(int previousValue, int nextValue, double ratio)
    {
        var interpolatedValue = previousValue + ((nextValue - previousValue) * ratio);
        return (int)Math.Round(interpolatedValue, MidpointRounding.AwayFromZero);
    }

    private static decimal InterpolateDecimal(decimal previousValue, decimal nextValue, double ratio)
    {
        var ratioDecimal = (decimal)ratio;
        var interpolatedValue = previousValue + ((nextValue - previousValue) * ratioDecimal);
        return decimal.Round(interpolatedValue, 2, MidpointRounding.AwayFromZero);
    }
}
