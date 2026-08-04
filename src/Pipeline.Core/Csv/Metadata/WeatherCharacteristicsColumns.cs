// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv.Metadata;

public static class WeatherCharacteristicsColumns
{
    private static readonly IReadOnlyList<(WeatherCharacteristics Flag, string ColumnName)> AllColumns =
        Enum.GetValues<WeatherCharacteristics>()
            .Where(flag => flag != WeatherCharacteristics.None)
            .Select(flag => (Flag: flag, ColumnName: EnumDisplayNameFormatter.ToDisplayName(flag)))
            .OrderBy(pair => pair.ColumnName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyList<(WeatherCharacteristics Flag, string ColumnName)> All => AllColumns;
}
