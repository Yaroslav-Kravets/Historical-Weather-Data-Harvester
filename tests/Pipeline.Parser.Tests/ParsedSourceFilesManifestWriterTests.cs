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

public sealed class ParsedSourceFilesManifestWriterTests
{
    private readonly IFileSystem fileSystem;
    private readonly string outputDirectory;
    private readonly ParsedSourceFilesManifestWriter manifestWriter;

    public ParsedSourceFilesManifestWriterTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        this.outputDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        this.fileSystem.Directory.CreateDirectory(this.outputDirectory);
        var csvRecordWriter = new CsvRecordWriter(this.fileSystem);
        this.manifestWriter = new ParsedSourceFilesManifestWriter(
            NullLogger<ParsedSourceFilesManifestWriter>.Instance,
            this.fileSystem,
            csvRecordWriter);
    }

    [Fact]
    public void Write_WritesManifestAtCoreFileNameWithExpectedRows()
    {
        var date = new DateTime(2003, 1, 1);
        var entries = new List<ParsedSourceFileEntry>
        {
            new("Kyiv", date, "/mnt/Weather/Kyiv/2003-01-01.html"),
            new("Kharkiv", date, "/mnt/Weather/Kharkiv/2003-01-01.html"),
        };

        this.manifestWriter.Write(entries, this.outputDirectory);

        var manifestPath = this.fileSystem.Path.Combine(this.outputDirectory, WeatherCsvOutputPaths.ParsedSourceFilesManifestFileName);
        Assert.True(this.fileSystem.File.Exists(manifestPath));

        using var reader = new StreamReader(this.fileSystem.File.OpenRead(manifestPath));
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<ParsedSourceFileManifestRecord>().ToList();

        Assert.Equal(2, records.Count);
        Assert.Equal("Kharkiv", records[0].Place);
        Assert.Equal("/mnt/Weather/Kharkiv/2003-01-01.html", records[0].SourceFilePath);
        Assert.Equal("Kyiv", records[1].Place);
        Assert.Equal("/mnt/Weather/Kyiv/2003-01-01.html", records[1].SourceFilePath);
    }
}
