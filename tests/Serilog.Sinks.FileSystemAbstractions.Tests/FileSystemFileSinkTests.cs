// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Serilog.Sinks.FileSystemAbstractions.Tests;

using FileSystem.TestSupport;
using Xunit;

public sealed class FileSystemFileSinkTests
{
    [Fact]
    public void Emit_WritesFormattedLineToInjectedFileSystem()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logPath = InMemoryFileSystem.UnderRoot(fileSystem, "logs", "stage.log");
        const string message = "pipeline stage started";

        using (var logger = new LoggerConfiguration()
            .WriteTo.File(fileSystem, logPath)
            .CreateLogger())
        {
            logger.Information(message);
        }

        Assert.True(fileSystem.File.Exists(logPath));
        var content = fileSystem.File.ReadAllText(logPath);
        Assert.Contains(message, content, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_FlushesNonFatalEventsWhenNotBuffered()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logPath = InMemoryFileSystem.UnderRoot(fileSystem, "logs", "flush.log");
        const string message = "visible before dispose";

        using var logger = new LoggerConfiguration()
            .WriteTo.File(fileSystem, logPath)
            .CreateLogger();
        logger.Information(message);

        // Default Serilog FileSink behavior: flush after each non-buffered emit.
        Assert.Contains(message, fileSystem.File.ReadAllText(logPath), StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_FlushesFatalEventsWhileLoggerIsAlive()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logPath = InMemoryFileSystem.UnderRoot(fileSystem, "logs", "fatal.log");
        const string message = "fatal visible before dispose";

        using var logger = new LoggerConfiguration()
            .WriteTo.File(fileSystem, logPath)
            .CreateLogger();
        logger.Fatal(message);

        // Fatal events use FlushToDisk (writer + underlying stream), not just writer.Flush.
        Assert.Contains(message, fileSystem.File.ReadAllText(logPath), StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_AppendsAcrossLoggerLifetimes()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logPath = InMemoryFileSystem.UnderRoot(fileSystem, "append.log");

        using (var first = new LoggerConfiguration()
            .WriteTo.File(fileSystem, logPath)
            .CreateLogger())
        {
            first.Information("first-line");
        }

        using (var second = new LoggerConfiguration()
            .WriteTo.File(fileSystem, logPath)
            .CreateLogger())
        {
            second.Information("second-line");
        }

        var content = fileSystem.File.ReadAllText(logPath);
        Assert.Contains("first-line", content, StringComparison.Ordinal);
        Assert.Contains("second-line", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_WritesOnlyToFileSystemPassedToSink()
    {
        var targetFileSystem = InMemoryFileSystem.Create();
        var otherFileSystem = InMemoryFileSystem.Create();
        var logPath = InMemoryFileSystem.UnderRoot(targetFileSystem, "stage.log");

        using (var logger = new LoggerConfiguration()
            .WriteTo.File(targetFileSystem, logPath)
            .CreateLogger())
        {
            logger.Information("target filesystem only");
        }

        Assert.True(targetFileSystem.File.Exists(logPath));
        Assert.False(otherFileSystem.File.Exists(logPath));
    }

    [Fact]
    public void Emit_CreatesParentDirectoryWhenMissing()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logPath = InMemoryFileSystem.UnderRoot(fileSystem, "nested", "dir", "stage.log");

        using (var logger = new LoggerConfiguration()
            .WriteTo.File(fileSystem, logPath)
            .CreateLogger())
        {
            logger.Information("creates directories");
        }

        Assert.True(fileSystem.Directory.Exists(fileSystem.Path.GetDirectoryName(logPath)!));
        Assert.True(fileSystem.File.Exists(logPath));
    }

    [Fact]
    public void AsyncWrapper_WritesThroughInjectedFileSystem()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logPath = InMemoryFileSystem.UnderRoot(fileSystem, "async-stage.log");
        const string message = "async wrapped log entry";

        using (var logger = new LoggerConfiguration()
            .WriteTo.Async(configure => configure.File(fileSystem, logPath))
            .CreateLogger())
        {
            logger.Information(message);
        }

        Assert.True(fileSystem.File.Exists(logPath));
        var content = fileSystem.File.ReadAllText(logPath);
        Assert.Contains(message, content, StringComparison.Ordinal);
    }
}
