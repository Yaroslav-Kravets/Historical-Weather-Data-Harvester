// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser.Tests;

using System.Globalization;
using System.IO.Abstractions;
using CsvHelper;
using FileSystem.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class ParsedStageManifestCsvWriterTests
{
    private const string KyivEnglishName = "Kyiv";
    private const string KyivNameInHtml = "Киеве";

    private readonly IFileSystem fileSystem;
    private readonly string outputDirectory;
    private readonly ParsedStageManifestCsvWriter parsedStageManifestCsvWriter;

    public ParsedStageManifestCsvWriterTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        this.outputDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        this.fileSystem.Directory.CreateDirectory(this.outputDirectory);
        var csvRecordWriter = new CsvRecordWriter(this.fileSystem);
        this.parsedStageManifestCsvWriter = new ParsedStageManifestCsvWriter(
            NullLogger<ParsedStageManifestCsvWriter>.Instance,
            this.fileSystem,
            csvRecordWriter);
    }

    [Fact]
    public void WriteParsedPlacesManifest_WritesProvidedPlaces()
    {
        var parsedPlaces = new List<(string EnglishName, string NameInHtml)>
        {
            (KyivEnglishName, KyivNameInHtml),
            ("Kharkiv", "Харькове"),
        };

        this.parsedStageManifestCsvWriter.WriteParsedPlacesManifest(parsedPlaces, this.outputDirectory);

        var rows = this.ReadCsv(this.CsvPath(WeatherCsvOutputPaths.ParsedPlacesManifestFileName));
        AssertManifestHeaders(rows);
        Assert.Equal(new[] { KyivEnglishName, KyivNameInHtml }, rows[1]);
        Assert.Equal(new[] { "Kharkiv", "Харькове" }, rows[2]);
    }

    [Fact]
    public void WriteWeatherCharacteristicsManifest_WritesProvidedCharacteristics()
    {
        var parsedCharacteristics = new List<(string EnglishName, string NameInHtml)>
        {
            ("Clear", "ясно"),
            ("Rain", "дождь"),
        };

        this.parsedStageManifestCsvWriter.WriteWeatherCharacteristicsManifest(parsedCharacteristics, this.outputDirectory);

        var rows = this.ReadCsv(this.CsvPath(WeatherCsvOutputPaths.WeatherCharacteristicsManifestFileName));
        AssertManifestHeaders(rows);
        Assert.Equal(3, rows.Count);
        AssertManifestRow(FindManifestRow(rows, "Clear"), "Clear", "ясно");
        AssertManifestRow(FindManifestRow(rows, "Rain"), "Rain", "дождь");
    }

    [Fact]
    public void WriteWeatherCharacteristicsManifest_WritesHeadersOnlyWhenEmpty()
    {
        this.parsedStageManifestCsvWriter.WriteWeatherCharacteristicsManifest([], this.outputDirectory);

        var rows = this.ReadCsv(this.CsvPath(WeatherCsvOutputPaths.WeatherCharacteristicsManifestFileName));
        AssertManifestHeaders(rows);
        Assert.Single(rows);
    }

    private static void AssertManifestHeaders(IReadOnlyList<string[]> rows)
    {
        Assert.NotEmpty(rows);
        Assert.Equal(WeatherCsvColumns.ManifestColumns, rows[0]);
    }

    private static void AssertManifestRow(string[] row, string englishName, string nameInHtml)
    {
        Assert.Equal(new[] { englishName, nameInHtml }, row);
    }

    private static string[] FindManifestRow(IReadOnlyList<string[]> rows, string englishName) =>
        rows.First(row =>
            row.Length >= 2 &&
            string.Equals(row[0], englishName, StringComparison.OrdinalIgnoreCase));

    private string CsvPath(string fileName) => this.fileSystem.Path.Combine(this.outputDirectory, fileName);

    private List<string[]> ReadCsv(string csvPath)
    {
        using var reader = new StreamReader(this.fileSystem.File.OpenRead(csvPath));
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
}
