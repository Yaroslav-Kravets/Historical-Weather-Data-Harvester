// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser.Tests;

using System.IO.Abstractions;
using System.Text;
using FileSystem.TestSupport;
using HtmlLog;
using Microsoft.Extensions.Logging.Abstractions;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.SevenZip;
using Xunit;

public sealed class ParsingPipelineTests
{
    private readonly IFileSystem fileSystem;
    private readonly string outputDirectory;
    private readonly ParsingPipeline parsingPipeline;

    static ParsingPipelineTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ParsingPipelineTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        this.outputDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        this.fileSystem.Directory.CreateDirectory(this.outputDirectory);
        this.parsingPipeline = CreatePipeline(this.fileSystem);
    }

    [Fact]
    public void Run_ThrowsDirectoryNotFoundException_WhenRootDirectoryDoesNotExist()
    {
        var missingRoot = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        var htmlReportPath = this.fileSystem.Path.Combine(this.outputDirectory, "result.html");
        var options = new ParsingRunOptions(missingRoot, this.outputDirectory, htmlReportPath, RunInParallel: false);

        var exception = Assert.Throws<DirectoryNotFoundException>(() => this.parsingPipeline.Run(options));

        Assert.Contains("Input directory not found", exception.Message, StringComparison.Ordinal);
        Assert.Contains(missingRoot, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ThrowsInvalidOperationException_WhenSevenZipSourceAndParallelEnabled()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.fileSystem, "weather.7z");
        WriteSevenZipArchive(this.fileSystem, archivePath, ("Kyiv/2003-01-01.html", BuildMinimalArchiveHtml("Киеве")));
        var htmlReportPath = this.fileSystem.Path.Combine(this.outputDirectory, "result.html");
        var options = new ParsingRunOptions(archivePath, this.outputDirectory, htmlReportPath, RunInParallel: true);

        var exception = Assert.Throws<InvalidOperationException>(() => this.parsingPipeline.Run(options));

        Assert.Contains("do not support parallel parsing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ParsesDirectorySource_WhenParallelEnabled()
    {
        var sourceRoot = InMemoryFileSystem.UnderRoot(this.fileSystem, "html");
        var placeDir = this.fileSystem.Path.Combine(sourceRoot, "Kyiv");
        this.fileSystem.Directory.CreateDirectory(placeDir);
        this.fileSystem.File.WriteAllText(
            this.fileSystem.Path.Combine(placeDir, "2003-01-01.html"),
            BuildMinimalArchiveHtml("Киеве", day: 1, rowCount: 1),
            Encoding.GetEncoding(1251));
        this.fileSystem.File.WriteAllText(
            this.fileSystem.Path.Combine(placeDir, "2003-01-02.html"),
            BuildMinimalArchiveHtml("Киеве", day: 2, rowCount: 3),
            Encoding.GetEncoding(1251));
        var htmlReportPath = this.fileSystem.Path.Combine(this.outputDirectory, "result.html");
        var options = new ParsingRunOptions(sourceRoot, this.outputDirectory, htmlReportPath, RunInParallel: true);

        this.parsingPipeline.Run(options);

        var csvPath = this.fileSystem.Path.Combine(
            this.outputDirectory,
            WeatherCsvOutputPaths.NormalizedColumnsDirectoryName,
            "Kyiv.csv");
        Assert.True(this.fileSystem.File.Exists(csvPath));
    }

    [Fact]
    public void Run_ThrowsFileNotFoundException_WhenSevenZipArchiveMissing()
    {
        var missingArchive = InMemoryFileSystem.UnderRoot(this.fileSystem, "missing.7z");
        var htmlReportPath = this.fileSystem.Path.Combine(this.outputDirectory, "result.html");
        var options = new ParsingRunOptions(missingArchive, this.outputDirectory, htmlReportPath, RunInParallel: false);

        var exception = Assert.Throws<FileNotFoundException>(() => this.parsingPipeline.Run(options));

        Assert.Contains("Input 7z archive not found", exception.Message, StringComparison.Ordinal);
        Assert.Contains(missingArchive, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_Throws_WhenSevenZipArchiveIsMalformed()
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.fileSystem, "broken.7z");
        this.fileSystem.Directory.CreateDirectory(this.fileSystem.Path.GetDirectoryName(archivePath)!);
        this.fileSystem.File.WriteAllBytes(archivePath, [0x00, 0x01, 0x02, 0x03]);
        var htmlReportPath = this.fileSystem.Path.Combine(this.outputDirectory, "result.html");
        var options = new ParsingRunOptions(archivePath, this.outputDirectory, htmlReportPath, RunInParallel: false);

        Assert.ThrowsAny<Exception>(() => this.parsingPipeline.Run(options));
    }

    [Theory]
    [InlineData("weather.7z")]
    [InlineData("weather.7Z")]
    public void Run_ParsesNestedEntriesFromSevenZipArchiveWithoutExtraction(string archiveFileName)
    {
        var archivePath = InMemoryFileSystem.UnderRoot(this.fileSystem, archiveFileName);
        WriteSevenZipArchive(
            this.fileSystem,
            archivePath,
            ("Kyiv/2003-01-01.html", BuildMinimalArchiveHtml("Киеве", day: 1, rowCount: 1)),
            ("Kyiv/2003-01-02.html", BuildMinimalArchiveHtml("Киеве", day: 2, rowCount: 3)));
        var htmlReportPath = this.fileSystem.Path.Combine(this.outputDirectory, "result.html");
        var options = new ParsingRunOptions(archivePath, this.outputDirectory, htmlReportPath, RunInParallel: false);

        this.parsingPipeline.Run(options);

        var csvPath = this.fileSystem.Path.Combine(
            this.outputDirectory,
            WeatherCsvOutputPaths.NormalizedColumnsDirectoryName,
            "Kyiv.csv");
        Assert.True(this.fileSystem.File.Exists(csvPath));

        var manifestPath = this.fileSystem.Path.Combine(
            this.outputDirectory,
            WeatherCsvOutputPaths.ParsedSourceFilesManifestFileName);
        Assert.True(this.fileSystem.File.Exists(manifestPath));
        var manifestText = this.fileSystem.File.ReadAllText(manifestPath);
        Assert.Contains("Kyiv/2003-01-01.html", manifestText, StringComparison.Ordinal);
        Assert.Contains("Kyiv/2003-01-02.html", manifestText, StringComparison.Ordinal);
        Assert.DoesNotContain(".extracted", manifestText, StringComparison.OrdinalIgnoreCase);

        var reportHtml = this.fileSystem.File.ReadAllText(htmlReportPath);
        Assert.Contains($"{archiveFileName}!Kyiv/2003-01-01.html", reportHtml, StringComparison.Ordinal);
        Assert.Contains($"{archiveFileName}!Kyiv/2003-01-02.html", reportHtml, StringComparison.Ordinal);
        var bogusEntryUri = new Uri(this.fileSystem.Path.GetFullPath("Kyiv/2003-01-01.html")).AbsoluteUri;
        Assert.DoesNotContain(bogusEntryUri, reportHtml, StringComparison.Ordinal);
    }

    private static ParsingPipeline CreatePipeline(IFileSystem fileSystem)
    {
        var csvRecordWriter = new CsvRecordWriter(fileSystem);
        var placeCsvFileNameResolver = new PlaceCsvFileNameResolver(fileSystem);
        var htmlLogFileManager = new HtmlLogFileManager(fileSystem);
        var placeConverter = new PlaceConverter();
        var weatherCharacteristicConverter = new WeatherCharacteristicConverter();
        var weatherDataCsvRecordMap = new WeatherDataCsvRecordMap(
            new WeatherCharacteristicsEnglishCsvConverter(weatherCharacteristicConverter));
        return new ParsingPipeline(
            NullLogger<ParsingPipeline>.Instance,
            fileSystem,
            new RealWeatherHtmlParser(
                fileSystem,
                NullLogger<RealWeatherHtmlParser>.Instance,
                weatherCharacteristicConverter),
            new HtmlFileParser(NullLogger<HtmlFileParser>.Instance),
            new ParseResultOrganizer(NullLogger<ParseResultOrganizer>.Instance, placeConverter),
            new ParsedFileInfoFlattener(),
            new ParsedWeatherCharacteristicsCollector(weatherCharacteristicConverter),
            new NormalizedColumnsWeatherDataCsvWriter(
                NullLogger<NormalizedColumnsWeatherDataCsvWriter>.Instance,
                fileSystem,
                csvRecordWriter,
                placeCsvFileNameResolver,
                weatherDataCsvRecordMap),
            new ParsedStageManifestCsvWriter(
                NullLogger<ParsedStageManifestCsvWriter>.Instance,
                fileSystem,
                csvRecordWriter),
            new ParsedSourceFilesManifestWriter(
                NullLogger<ParsedSourceFilesManifestWriter>.Instance,
                fileSystem,
                csvRecordWriter),
            new ParsingReportWriter(
                NullLogger<ParsingReportWriter>.Instance,
                new ParsingPlaceErrorCountsBuilder(),
                fileSystem,
                weatherCharacteristicConverter),
            htmlLogFileManager,
            placeConverter);
    }

    private static void WriteSevenZipArchive(
        IFileSystem fileSystem,
        string archivePath,
        params (string EntryPath, string Content)[] entries)
    {
        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(archivePath)!);
        using var archiveStream = fileSystem.File.Create(archivePath);
        using var writer = WriterFactory.OpenWriter(
            archiveStream,
            ArchiveType.SevenZip,
            new SevenZipWriterOptions());
        foreach (var (entryPath, content) in entries)
        {
            var bytes = Encoding.GetEncoding(1251).GetBytes(content);
            using var entryStream = new MemoryStream(bytes);
            writer.Write(entryPath, entryStream, null);
        }
    }

    private static string BuildMinimalArchiveHtml(string cityNameInHtml, int day = 1, int rowCount = 1)
    {
        var rows = new StringBuilder();
        for (var i = 0; i < rowCount; i++)
        {
            rows.AppendLine(
                $"""
                <tr>
                <td class="at_l at_time">{i:D2}:00</td>
                <td><div class="ov_hide">ясно</div></td>
                <td>-12°C</td>
                <td><img alt="северный" /> 2</td>
                <td>750</td>
                <td>70</td>
                </tr>
                """);
        }

        return $"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta http-equiv="Content-Type" content="text/html; charset=windows-1251">
        <title>Архив погоды в {cityNameInHtml}. Погода за {day} январь 2003 года</title>
        </head>
        <body>
        <table class="archive_table table">
        {rows}
        </table>
        </body>
        </html>
        """;
    }
}
