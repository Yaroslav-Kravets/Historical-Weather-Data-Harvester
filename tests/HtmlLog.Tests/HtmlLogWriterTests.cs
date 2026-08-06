// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLog.Tests;

using FileSystem.TestSupport;
using Xunit;

public sealed class HtmlLogWriterTests
{
    [Fact]
    public void WriteTable_EscapesMaliciousTagInCell_ButLeavesTrustedAnchor()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var reportDirectory = InMemoryFileSystem.UnderRoot(fileSystem, Guid.NewGuid().ToString("N"));
        fileSystem.Directory.CreateDirectory(reportDirectory);
        var reportPath = fileSystem.Path.Combine(reportDirectory, "result.html");

        using var fileManager = new HtmlLogFileManager(fileSystem);
        using (var writer = new HtmlLogWriter(fileManager, reportPath, "Encoding Test"))
        {
            writer.WriteTable(
                new[]
                {
                    new
                    {
                        Malicious = "<img src=x onerror=alert(1)>",
                        Anchor = "<a href=\"file:///tmp/a.html\" target=\"_blank\">a.html</a>",
                    },
                },
                "Cells");
        }

        var html = fileSystem.File.ReadAllText(reportPath);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img src=x onerror=", html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"file:///tmp/a.html\" target=\"_blank\">a.html</a>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteDistributionDiagram_EmbedsPngWithoutTouchingRealDisk()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var reportDirectory = InMemoryFileSystem.UnderRoot(fileSystem, Guid.NewGuid().ToString("N"));
        fileSystem.Directory.CreateDirectory(reportDirectory);
        var reportPath = fileSystem.Path.Combine(reportDirectory, "result.html");

        using var fileManager = new HtmlLogFileManager(fileSystem);
        using (var writer = new HtmlLogWriter(fileManager, reportPath, "Diagram Test"))
        {
            writer.WriteDistributionDiagram(
                new[] { 1.0, 2.0, 2.0, 3.0, 5.0 },
                caption: "Sample Distribution",
                width: 200,
                height: 100,
                bins: 5);
        }

        Assert.True(fileSystem.File.Exists(reportPath));
        var html = fileSystem.File.ReadAllText(reportPath);
        Assert.Contains("data:image/png;base64,", html, StringComparison.Ordinal);
        Assert.Contains("Sample Distribution", html, StringComparison.Ordinal);
        Assert.Contains("image-container", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_ThrowsWhenSecondWriterOpensSamePath()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var reportDirectory = InMemoryFileSystem.UnderRoot(fileSystem, Guid.NewGuid().ToString("N"));
        fileSystem.Directory.CreateDirectory(reportDirectory);
        var reportPath = fileSystem.Path.Combine(reportDirectory, "result.html");

        using var fileManager = new HtmlLogFileManager(fileSystem);
        using var first = new HtmlLogWriter(fileManager, reportPath, "First");

        var ex = Assert.Throws<InvalidOperationException>(
            () => new HtmlLogWriter(fileManager, reportPath, "Second"));

        Assert.Contains(reportPath, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_ThrowsWhenSecondWriterOpensEquivalentPathViaRelativeSegments()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var reportDirectory = InMemoryFileSystem.UnderRoot(fileSystem, Guid.NewGuid().ToString("N"));
        fileSystem.Directory.CreateDirectory(reportDirectory);
        var reportPath = fileSystem.Path.Combine(reportDirectory, "result.html");
        var equivalentPath = fileSystem.Path.Combine(reportDirectory, ".", "result.html");

        using var fileManager = new HtmlLogFileManager(fileSystem);
        using var first = new HtmlLogWriter(fileManager, reportPath, "First");

        var ex = Assert.Throws<InvalidOperationException>(
            () => new HtmlLogWriter(fileManager, equivalentPath, "Second"));

        Assert.Contains("already open", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_AllowsReopenAfterDispose()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var reportDirectory = InMemoryFileSystem.UnderRoot(fileSystem, Guid.NewGuid().ToString("N"));
        fileSystem.Directory.CreateDirectory(reportDirectory);
        var reportPath = fileSystem.Path.Combine(reportDirectory, "result.html");

        using var fileManager = new HtmlLogFileManager(fileSystem);
        using (var first = new HtmlLogWriter(fileManager, reportPath, "First"))
        {
            first.WriteTable(new[] { new { Name = "a" } }, "Table");
        }

        using var second = new HtmlLogWriter(fileManager, reportPath, "Second");
        second.WriteTable(new[] { new { Name = "b" } }, "Table");
    }

    [Fact]
    public void RenderTableHtml_ReturnsTableFragmentWithoutDocumentChrome()
    {
        var html = HtmlLogWriter.RenderTableHtml(
            new[] { new { EnglishName = "Clear", RowCount = 1 } },
            "Available Weather Characteristics");

        Assert.Contains("table-container", html, StringComparison.Ordinal);
        Assert.Contains("Available Weather Characteristics", html, StringComparison.Ordinal);
        Assert.Contains("English Name", html, StringComparison.Ordinal);
        Assert.Contains("Clear", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<!DOCTYPE html>", html, StringComparison.Ordinal);
        Assert.DoesNotContain(HtmlLogWriter.FooterStartMarker, html, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderTableHtml_ReturnsEmpty_WhenItemsEmpty()
    {
        Assert.Equal(string.Empty, HtmlLogWriter.RenderTableHtml(Array.Empty<object>(), "Empty"));
    }
}
