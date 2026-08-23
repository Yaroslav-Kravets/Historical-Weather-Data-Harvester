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

public sealed class AnalysisReportWriterTests
{
    private readonly IFileSystem fileSystem = InMemoryFileSystem.Create();

    [Fact]
    public void Write_WritesCoverageTableBeforeUsageTableBeforeFooter()
    {
        var reportPath = InMemoryFileSystem.UnderRoot(this.fileSystem, "result.html");
        this.fileSystem.Directory.CreateDirectory(this.fileSystem.Path.GetDirectoryName(reportPath)!);

        using var fileManager = new HtmlLogFileManager(this.fileSystem);
        this.CreateWriter().Write(
            [
                new PlaceDateCoverageRow("Kyiv", "2003-01-01", "2003-01-04", 3, 2, "2003-01-02, 03"),
            ],
            [
                new WeatherCharacteristicUsageRow("Clear", "ясно", 1, 100.0),
            ],
            fileManager,
            reportPath);

        var html = this.fileSystem.File.ReadAllText(reportPath);
        Assert.Contains("Place Date Coverage", html, StringComparison.Ordinal);
        Assert.Contains("Weather Characteristics Usage", html, StringComparison.Ordinal);
        Assert.Contains("Observed Days", html, StringComparison.Ordinal);
        Assert.Contains("Skipped Days", html, StringComparison.Ordinal);
        Assert.Contains("Skipped Dates", html, StringComparison.Ordinal);
        Assert.Contains("2003-01-01", html, StringComparison.Ordinal);
        Assert.Contains("2003-01-04", html, StringComparison.Ordinal);
        Assert.Contains("2003-01-02, 03", html, StringComparison.Ordinal);

        // HtmlLogWriter renders numeric cells as <td class="numeric">N</td>.
        Assert.Contains(">3</td>", html, StringComparison.Ordinal);
        Assert.Contains(">2</td>", html, StringComparison.Ordinal);
        Assert.Contains("ясно", html, StringComparison.Ordinal);
        Assert.Contains("100.00000%", html, StringComparison.Ordinal);
        Assert.Contains("End of summary report", html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("Place Date Coverage", StringComparison.Ordinal)
            < html.IndexOf("Weather Characteristics Usage", StringComparison.Ordinal));
        Assert.True(
            html.IndexOf("Weather Characteristics Usage", StringComparison.Ordinal)
            < html.IndexOf("End of summary report", StringComparison.Ordinal));
    }

    [Fact]
    public void Write_WritesCoverageTableOnly_WhenUsageRowsEmpty()
    {
        var reportPath = InMemoryFileSystem.UnderRoot(this.fileSystem, "coverage-only.html");
        this.fileSystem.Directory.CreateDirectory(this.fileSystem.Path.GetDirectoryName(reportPath)!);

        using var fileManager = new HtmlLogFileManager(this.fileSystem);
        this.CreateWriter().Write(
            [
                new PlaceDateCoverageRow("Kyiv", "2003-01-01", "2003-01-01", 1, 0, string.Empty),
            ],
            [],
            fileManager,
            reportPath);

        var html = this.fileSystem.File.ReadAllText(reportPath);
        Assert.Contains("Place Date Coverage", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Weather Characteristics Usage", html, StringComparison.Ordinal);
        Assert.Contains("End of summary report", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_DoesNotCreateReport_WhenNoRows()
    {
        var reportPath = InMemoryFileSystem.UnderRoot(this.fileSystem, "empty.html");
        this.fileSystem.Directory.CreateDirectory(this.fileSystem.Path.GetDirectoryName(reportPath)!);

        using var fileManager = new HtmlLogFileManager(this.fileSystem);
        this.CreateWriter().Write([], [], fileManager, reportPath);

        Assert.False(this.fileSystem.File.Exists(reportPath));
    }

    private AnalysisReportWriter CreateWriter() =>
        new(NullLogger<AnalysisReportWriter>.Instance);
}
