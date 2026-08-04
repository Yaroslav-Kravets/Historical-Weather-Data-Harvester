// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer.Tests;

using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using CsvHelper;
using FileSystem.TestSupport;
using Xunit;

public sealed class ParsedSourceFilesManifestReaderTests
{
    private readonly IFileSystem fileSystem;
    private readonly string outputDirectory;
    private readonly ParsedSourceFilesManifestReader manifestReader;

    public ParsedSourceFilesManifestReaderTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        this.outputDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        this.fileSystem.Directory.CreateDirectory(this.outputDirectory);
        this.manifestReader = new ParsedSourceFilesManifestReader(this.fileSystem);
    }

    [Fact]
    public void ResolveSourceFilePath_FallsBackWhenManifestMissing()
    {
        var date = new DateTime(2003, 1, 1);
        var manifest = new Dictionary<string, Dictionary<DateTime, string>>(StringComparer.OrdinalIgnoreCase);

        var resolved = this.manifestReader.ResolveSourceFilePath(manifest, "Kyiv", date);

        Assert.Equal("Kyiv/2003-01-01", resolved);
    }

    [Fact]
    public void ReadByPlaceAndDate_ReadsManifestWrittenAtCoreFileName()
    {
        var date = new DateTime(2003, 1, 1);
        var records = new List<ParsedSourceFileManifestRecord>
        {
            new() { Place = "Kyiv", Date = date, SourceFilePath = "/mnt/Weather/Kyiv/2003-01-01.html" },
            new() { Place = "Kharkiv", Date = date, SourceFilePath = "/mnt/Weather/Kharkiv/2003-01-01.html" },
        };

        this.WriteManifest(records);

        var manifest = this.manifestReader.ReadByPlaceAndDate(this.outputDirectory);

        Assert.Equal("/mnt/Weather/Kyiv/2003-01-01.html", manifest["Kyiv"][date]);
        Assert.Equal("/mnt/Weather/Kharkiv/2003-01-01.html", manifest["Kharkiv"][date]);
    }

    private void WriteManifest(IReadOnlyList<ParsedSourceFileManifestRecord> records)
    {
        var manifestPath = this.fileSystem.Path.Combine(this.outputDirectory, WeatherCsvOutputPaths.ParsedSourceFilesManifestFileName);
        using var writer = new StreamWriter(this.fileSystem.File.Create(manifestPath), Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(records);
    }
}
