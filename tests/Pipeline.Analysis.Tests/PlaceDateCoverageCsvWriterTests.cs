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
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class PlaceDateCoverageCsvWriterTests
{
    private readonly IFileSystem fileSystem;
    private readonly string outputDirectory;
    private readonly PlaceDateCoverageCsvWriter writer;

    public PlaceDateCoverageCsvWriterTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        this.outputDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        this.fileSystem.Directory.CreateDirectory(this.outputDirectory);
        this.writer = new PlaceDateCoverageCsvWriter(
            NullLogger<PlaceDateCoverageCsvWriter>.Instance,
            this.fileSystem,
            new CsvRecordWriter(this.fileSystem));
    }

    [Fact]
    public void Write_WritesHeadersOnlyWhenEmpty()
    {
        this.writer.Write([], this.outputDirectory);

        var csvPath = this.CsvPath();
        Assert.True(this.fileSystem.File.Exists(csvPath));

        var csv = this.fileSystem.File.ReadAllText(csvPath);
        Assert.Equal(
            "Place,FirstDate,LastDate,ObservedDays,SkippedDays,SkippedDates",
            csv.TrimEnd('\r', '\n'));
    }

    [Fact]
    public void Write_MapsNullDatesToEmptyStrings()
    {
        this.writer.Write(
            [
                new PlaceDateCoverageRow("Kharkiv", null, null, 0, 0, string.Empty),
                new PlaceDateCoverageRow("Kyiv", "2003-01-01", "2003-01-04", 3, 2, "2003-01-02, 03"),
            ],
            this.outputDirectory);

        var csv = this.fileSystem.File.ReadAllText(this.CsvPath());
        Assert.Contains("Kharkiv,,,0,0,", csv, StringComparison.Ordinal);
        Assert.Contains(
            "Kyiv,2003-01-01,2003-01-04,3,2,\"2003-01-02, 03\"",
            csv,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Write_UsesPlaceDateCoverageFileNameInStageDirectory()
    {
        this.writer.Write(
            [new PlaceDateCoverageRow("Kyiv", "2003-01-01", "2003-01-01", 1, 0, string.Empty)],
            this.outputDirectory);

        Assert.True(this.fileSystem.File.Exists(this.CsvPath()));
    }

    private string CsvPath() =>
        this.fileSystem.Path.Combine(this.outputDirectory, WeatherCsvOutputPaths.PlaceDateCoverageFileName);
}
