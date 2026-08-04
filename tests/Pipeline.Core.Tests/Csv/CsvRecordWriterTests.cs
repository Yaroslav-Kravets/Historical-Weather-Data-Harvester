// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv;

using CsvHelper.Configuration.Attributes;
using Pipeline.Core.Tests.Csv.TestSupport;
using Xunit;

public sealed class CsvRecordWriterTests
{
    private readonly CsvTestContext testContext;
    private readonly CsvRecordWriter writer;
    private readonly string outputDirectory;

    public CsvRecordWriterTests()
    {
        this.testContext = new CsvTestContext();
        this.outputDirectory = this.testContext.EnsureDirectoryUnderRoot("writer-output");
        this.writer = this.testContext.CsvRecordWriter;
    }

    [Fact]
    public void WriteRecords_ReturnsZeroForEmptyRecords()
    {
        var rowCount = this.writer.WriteRecords(this.outputDirectory, "empty.csv", Array.Empty<SampleRecord>());

        Assert.Equal(0, rowCount);
        Assert.True(this.testContext.FileSystem.File.Exists(this.testContext.PathUnderRoot("writer-output", "empty.csv")));
    }

    [Fact]
    public void WriteRecords_WritesRecordsAndReturnsCount()
    {
        var records = new[]
        {
            new SampleRecord { Name = "first" },
            new SampleRecord { Name = "second" },
        };

        var rowCount = this.writer.WriteRecords(this.outputDirectory, "rows.csv", records);
        var rows = CsvTableAssertions.ReadRows(
            this.testContext.FileSystem,
            this.testContext.PathUnderRoot("writer-output", "rows.csv"));

        Assert.Equal(2, rowCount);
        Assert.Equal(
            new[] { "first", "second" },
            rows.Skip(1).Select(row => CsvTableAssertions.GetRequiredValue(rows[0], row, "Name")).ToArray());
    }

    private sealed class SampleRecord
    {
        [Name("Name")]
        public string Name { get; init; } = string.Empty;
    }
}
