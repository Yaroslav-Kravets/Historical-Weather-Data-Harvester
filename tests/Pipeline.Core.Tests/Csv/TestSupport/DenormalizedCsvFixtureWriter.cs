// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv.TestSupport;

using System.Globalization;
using System.IO.Abstractions;

internal static class DenormalizedCsvFixtureWriter
{
    public static void WritePlaceFile(
        IFileSystem fileSystem,
        string csvPath,
        IReadOnlyList<WeatherDataRow> rows,
        bool includePlaceColumn = false,
        string placeName = "Kyiv")
    {
        var directory = fileSystem.Path.GetDirectoryName(csvPath)
            ?? throw new InvalidOperationException($"Unable to resolve directory for path '{csvPath}'.");
        fileSystem.Directory.CreateDirectory(directory);

        var headers = BuildHeader(includePlaceColumn);
        var lines = new List<string> { string.Join(',', headers) };

        foreach (var row in rows)
        {
            var cells = new List<string>();
            if (includePlaceColumn)
            {
                cells.Add(placeName);
            }

            cells.Add(row.Time.ToString(WeatherCsvColumns.DateTimeFormat, CultureInfo.InvariantCulture));
            cells.Add(row.Temperature.ToString(CultureInfo.InvariantCulture));
            cells.Add(row.WindDirectionAzimuth.ToString(CultureInfo.InvariantCulture));
            cells.Add(row.WindSpeed.ToString(CultureInfo.InvariantCulture));
            cells.Add(row.AtmosphericPressure.ToString(CultureInfo.InvariantCulture));
            cells.Add(row.Humidity.ToString(CultureInfo.InvariantCulture));

            foreach (var (flag, _) in WeatherCharacteristicsColumns.All)
            {
                cells.Add((row.WeatherCharacteristics & flag) == flag ? "1" : "0");
            }

            lines.Add(string.Join(',', cells));
        }

        fileSystem.File.WriteAllText(csvPath, string.Join('\n', lines) + '\n');
    }

    private static List<string> BuildHeader(bool includePlaceColumn)
    {
        var headers = new List<string>();
        if (includePlaceColumn)
        {
            headers.Add(WeatherCsvColumns.Place);
        }

        headers.AddRange(WeatherCsvColumns.ScalarColumns);
        headers.AddRange(WeatherCharacteristicsColumns.All.Select(pair => pair.ColumnName));
        return headers;
    }
}
