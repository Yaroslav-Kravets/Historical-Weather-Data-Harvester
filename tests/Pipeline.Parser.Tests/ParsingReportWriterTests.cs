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
using FileSystem.TestSupport;
using HtmlLog;
using Microsoft.Extensions.Logging.Abstractions;
using Pipeline.Core.Enums;
using Pipeline.Core.Models;
using Xunit;

public sealed class ParsingReportWriterTests
{
    [Fact]
    public void WriteReport_DirectoryExampleLink_HtmlEncodesMaliciousFileName()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var reportDirectory = InMemoryFileSystem.UnderRoot(fileSystem, Guid.NewGuid().ToString("N"));
        fileSystem.Directory.CreateDirectory(reportDirectory);
        var reportPath = fileSystem.Path.Combine(reportDirectory, "result.html");
        var sourceRoot = InMemoryFileSystem.UnderRoot(fileSystem, "html");
        fileSystem.Directory.CreateDirectory(sourceRoot);
        var maliciousName = "<img src=x onerror=alert(1)>.html";
        var filePath = fileSystem.Path.Combine(sourceRoot, "Kyiv", maliciousName);

        WriteMinimalReport(
            fileSystem,
            reportPath,
            sourcePath: sourceRoot,
            isSevenZipSource: false,
            filePath);

        var html = fileSystem.File.ReadAllText(reportPath);
        Assert.Contains("<a href=", html, StringComparison.Ordinal);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;.html", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img src=x onerror=", html, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteReport_SevenZipExample_EscapesAmpersandOnce()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var reportDirectory = InMemoryFileSystem.UnderRoot(fileSystem, Guid.NewGuid().ToString("N"));
        fileSystem.Directory.CreateDirectory(reportDirectory);
        var reportPath = fileSystem.Path.Combine(reportDirectory, "result.html");
        var archivePath = InMemoryFileSystem.UnderRoot(fileSystem, "Kyiv&Odesa.7z");
        var entryPath = "Kyiv/a&b.html";

        WriteMinimalReport(
            fileSystem,
            reportPath,
            sourcePath: archivePath,
            isSevenZipSource: true,
            entryPath);

        var html = fileSystem.File.ReadAllText(reportPath);
        Assert.Contains("Kyiv&amp;Odesa.7z!Kyiv/a&amp;b.html", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Kyiv&amp;amp;Odesa", html, StringComparison.Ordinal);
        Assert.DoesNotContain("a&amp;amp;b", html, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteReport_SevenZipExample_HtmlEncodesMaliciousEntryPath()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var reportDirectory = InMemoryFileSystem.UnderRoot(fileSystem, Guid.NewGuid().ToString("N"));
        fileSystem.Directory.CreateDirectory(reportDirectory);
        var reportPath = fileSystem.Path.Combine(reportDirectory, "result.html");
        var archivePath = InMemoryFileSystem.UnderRoot(fileSystem, "weather.7z");
        var entryPath = "Kyiv/<img src=x onerror=alert(1)>.html";

        WriteMinimalReport(
            fileSystem,
            reportPath,
            sourcePath: archivePath,
            isSevenZipSource: true,
            entryPath);

        var html = fileSystem.File.ReadAllText(reportPath);
        Assert.Contains("weather.7z!Kyiv/&lt;img src=x onerror=alert(1)&gt;.html", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img src=x onerror=", html, StringComparison.Ordinal);
    }

    private static void WriteMinimalReport(
        IFileSystem fileSystem,
        string reportPath,
        string sourcePath,
        bool isSevenZipSource,
        string filePath)
    {
        var date = new DateTime(2003, 1, 1);

        // WriteDistributionDiagram needs min != max across samples (default bins=30).
        var flattened = new List<ParsedFileInfo>();
        for (var i = 0; i < 40; i++)
        {
            var rows = Enumerable.Range(0, i + 1)
                .Select(hour => new WeatherDataRow(
                    date.AddHours(hour),
                    WeatherCharacteristics.Clear,
                    -12,
                    0,
                    2.0m,
                    750,
                    70))
                .ToList();
            var parseResult = new HtmlParseResult("Kyiv", "2003-01-01", rows);
            var path = i == 0 ? filePath : $"{filePath}.extra-{i}";
            flattened.Add(new ParsedFileInfo("Kyiv", date.AddDays(i), path, parseResult));
        }

        var reportWriter = new ParsingReportWriter(
            NullLogger<ParsingReportWriter>.Instance,
            new ParsingPlaceErrorCountsBuilder(),
            fileSystem,
            new WeatherCharacteristicConverter());

        using var fileManager = new HtmlLogFileManager(fileSystem);
        using (var htmlWriter = new HtmlLogWriter(fileManager, reportPath, "Encoding Test"))
        {
            reportWriter.WriteReport(
                htmlWriter,
                sourcePath,
                isSevenZipSource,
                totalFiles: 1,
                parsingSuccessfulCount: 1,
                parsingUnsuccessfulCount: 0,
                totalTimeSeconds: 1,
                averageTimePerFileSeconds: 1,
                resultsByPlace: new Dictionary<string, SortedDictionary<DateTime, ParsedDateEntry>>(StringComparer.OrdinalIgnoreCase),
                issueCollector: new ParsingIssueCollector(new PlaceConverter()),
                flattenedParseResults: flattened);
        }
    }
}
