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
    public void Write_WritesUsageTableBeforeFooter()
    {
        var reportPath = InMemoryFileSystem.UnderRoot(this.fileSystem, "result.html");
        this.fileSystem.Directory.CreateDirectory(this.fileSystem.Path.GetDirectoryName(reportPath)!);

        using var fileManager = new HtmlLogFileManager(this.fileSystem);
        using (var htmlWriter = new HtmlLogWriter(fileManager, reportPath, "Test"))
        {
            htmlWriter.WriteTable(
                new[] { new { Metric = "Placeholder", Value = "1" } },
                "Stage Placeholder");
            this.CreateWriter().Write(
                [
                    new WeatherCharacteristicUsageRow("Clear", "ясно", 1, 100.0),
                ],
                htmlWriter);
        }

        var html = this.fileSystem.File.ReadAllText(reportPath);
        Assert.Contains("Weather Characteristics Usage", html, StringComparison.Ordinal);
        Assert.Contains("ясно", html, StringComparison.Ordinal);
        Assert.Contains("100.00000%", html, StringComparison.Ordinal);
        Assert.Contains("Stage Placeholder", html, StringComparison.Ordinal);
        Assert.Contains("End of summary report", html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("Weather Characteristics Usage", StringComparison.Ordinal)
            < html.IndexOf("End of summary report", StringComparison.Ordinal));
    }

    [Fact]
    public void Write_DoesNothing_WhenNoRows()
    {
        var reportPath = InMemoryFileSystem.UnderRoot(this.fileSystem, "empty.html");
        this.fileSystem.Directory.CreateDirectory(this.fileSystem.Path.GetDirectoryName(reportPath)!);

        using var fileManager = new HtmlLogFileManager(this.fileSystem);
        using (var htmlWriter = new HtmlLogWriter(fileManager, reportPath, "Test"))
        {
            this.CreateWriter().Write([], htmlWriter);
        }

        var html = this.fileSystem.File.ReadAllText(reportPath);
        Assert.DoesNotContain("Weather Characteristics Usage", html, StringComparison.Ordinal);
        Assert.Contains("End of summary report", html, StringComparison.Ordinal);
    }

    private WeatherCharacteristicUsageReportWriter CreateWriter() =>
        new(NullLogger<WeatherCharacteristicUsageReportWriter>.Instance);
}
