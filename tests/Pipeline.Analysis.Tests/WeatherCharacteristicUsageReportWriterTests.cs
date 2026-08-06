// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis.Tests;

using System.IO.Abstractions;
using FileSystem.TestSupport;
using HtmlLog;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class WeatherCharacteristicUsageReportWriterTests
{
    private readonly IFileSystem fileSystem = InMemoryFileSystem.Create();

    [Fact]
    public void Write_InsertsTableBeforeFooter_WhenReportExists()
    {
        var reportPath = InMemoryFileSystem.UnderRoot(this.fileSystem, "result.html");
        this.WriteMinimalHtmlReport(reportPath);

        var writer = this.CreateWriter();
        writer.Write(
            [
                new WeatherCharacteristicUsageRow("Clear", "ясно", 1, 100.0),
            ],
            reportPath);

        var html = this.fileSystem.File.ReadAllText(reportPath);
        Assert.Contains("Available Weather Characteristics", html, StringComparison.Ordinal);
        Assert.Contains("ясно", html, StringComparison.Ordinal);
        Assert.Contains("100.00%", html, StringComparison.Ordinal);
        Assert.Contains("Stage Placeholder", html, StringComparison.Ordinal);
        Assert.Contains("End of summary report", html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("Available Weather Characteristics", StringComparison.Ordinal)
            < html.IndexOf("End of summary report", StringComparison.Ordinal));
    }

    [Fact]
    public void Write_ReplacesExistingUsageTable_WhenCalledTwice()
    {
        var reportPath = InMemoryFileSystem.UnderRoot(this.fileSystem, "result.html");
        this.WriteMinimalHtmlReport(reportPath);

        var writer = this.CreateWriter();
        writer.Write(
            [new WeatherCharacteristicUsageRow("Clear", "ясно", 1, 100.0)],
            reportPath);
        writer.Write(
            [new WeatherCharacteristicUsageRow("Rain", "дождь", 2, 50.0)],
            reportPath);

        var html = this.fileSystem.File.ReadAllText(reportPath);
        Assert.Equal(1, CountOccurrences(html, "Available Weather Characteristics"));
        Assert.Contains("дождь", html, StringComparison.Ordinal);
        Assert.Contains("50.00%", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ясно", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_CreatesFullReport_WhenFileMissing()
    {
        var reportPath = InMemoryFileSystem.UnderRoot(this.fileSystem, "missing-result.html");
        this.fileSystem.Directory.CreateDirectory(this.fileSystem.Path.GetDirectoryName(reportPath)!);

        var writer = this.CreateWriter();
        writer.Write(
            [
                new WeatherCharacteristicUsageRow("Rain", "дождь", 2, 50.0),
            ],
            reportPath);

        Assert.True(this.fileSystem.File.Exists(reportPath));
        var html = this.fileSystem.File.ReadAllText(reportPath);
        Assert.Contains("Available Weather Characteristics", html, StringComparison.Ordinal);
        Assert.Contains("дождь", html, StringComparison.Ordinal);
        Assert.Contains("End of summary report", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_Throws_WhenFooterMarkerMissing()
    {
        var reportPath = InMemoryFileSystem.UnderRoot(this.fileSystem, "broken.html");
        this.fileSystem.Directory.CreateDirectory(this.fileSystem.Path.GetDirectoryName(reportPath)!);
        this.fileSystem.File.WriteAllText(reportPath, "<html><body>no footer</body></html>");

        var writer = this.CreateWriter();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            writer.Write(
                [
                    new WeatherCharacteristicUsageRow("Clear", "ясно", 1, 100.0),
                ],
                reportPath));

        Assert.Contains("footer marker", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private WeatherCharacteristicUsageReportWriter CreateWriter() =>
        new(
            NullLogger<WeatherCharacteristicUsageReportWriter>.Instance,
            this.fileSystem,
            new HtmlLogFileManager(this.fileSystem));

    private void WriteMinimalHtmlReport(string reportPath)
    {
        var directory = this.fileSystem.Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(directory))
        {
            this.fileSystem.Directory.CreateDirectory(directory);
        }

        using var fileManager = new HtmlLogFileManager(this.fileSystem);
        using (var htmlWriter = new HtmlLogWriter(fileManager, reportPath, "Test"))
        {
            htmlWriter.WriteTable(
                new[] { new { Metric = "Placeholder", Value = "1" } },
                "Stage Placeholder");
        }
    }
}
