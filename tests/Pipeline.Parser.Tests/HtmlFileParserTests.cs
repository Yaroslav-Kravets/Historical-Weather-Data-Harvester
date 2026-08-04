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
using Microsoft.Extensions.Logging.Abstractions;
using Pipeline.SourceFileSystem;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.SevenZip;
using Xunit;

public sealed class HtmlFileParserTests
{
    [Fact]
    public void ParseFiles_Throws_WhenParallelAndSourceDoesNotSupportParallel()
    {
        var fileSystem = InMemoryFileSystem.Create();
        fileSystem.Directory.CreateDirectory(InMemoryFileSystem.Root);
        var archivePath = InMemoryFileSystem.UnderRoot(fileSystem, "weather.7z");
        WriteSevenZipArchive(fileSystem, archivePath, ("Kyiv/2003-01-01.html", "unused"));

        var parser = new HtmlFileParser(NullLogger<HtmlFileParser>.Instance);
        var htmlParser = new RealWeatherHtmlParser(
            fileSystem,
            NullLogger<RealWeatherHtmlParser>.Instance,
            new WeatherCharacteristicConverter());
        var issueCollector = new ParsingIssueCollector(new PlaceConverter());

        using var source = new SevenZipSourceFileSystem(fileSystem, archivePath);
        var exception = Assert.Throws<InvalidOperationException>(() =>
            parser.ParseFiles(
                source,
                htmlParser,
                issueCollector,
                runInParallel: true,
                out _,
                out _,
                out _));

        Assert.Contains("do not support parallel parsing", exception.Message, StringComparison.Ordinal);
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
            var bytes = Encoding.UTF8.GetBytes(content);
            using var entryStream = new MemoryStream(bytes);
            writer.Write(entryPath, entryStream, null);
        }
    }
}
