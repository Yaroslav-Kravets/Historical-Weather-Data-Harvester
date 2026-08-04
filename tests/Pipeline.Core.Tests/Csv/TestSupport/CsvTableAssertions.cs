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
using CsvHelper;
using Xunit;

internal static class CsvTableAssertions
{
    public static List<string[]> ReadRows(IFileSystem fileSystem, string csvPath)
    {
        using var reader = new StreamReader(fileSystem.File.OpenRead(csvPath));
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<string[]>();
        while (csv.Read())
        {
            var row = new string[csv.Parser.Count];
            for (var i = 0; i < csv.Parser.Count; i++)
            {
                row[i] = csv.GetField(i) ?? string.Empty;
            }

            rows.Add(row);
        }

        return rows;
    }

    public static string GetRequiredValue(
        IReadOnlyList<string> header,
        IReadOnlyList<string> row,
        string columnName)
    {
        var index = Array.FindIndex(header.ToArray(), column => string.Equals(column, columnName, StringComparison.Ordinal));
        Assert.True(index >= 0, $"Expected CSV to contain column '{columnName}'.");
        return row[index];
    }
}
