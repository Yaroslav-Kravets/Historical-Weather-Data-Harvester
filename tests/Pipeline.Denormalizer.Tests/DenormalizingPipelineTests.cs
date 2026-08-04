// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Denormalizer.Tests;

using System.Globalization;
using System.IO.Abstractions;
using CsvHelper;
using FileSystem.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class DenormalizingPipelineTests
{
    private readonly IFileSystem fileSystem;
    private readonly CsvRecordWriter csvRecordWriter;
    private readonly PlaceCsvFileNameResolver placeCsvFileNameResolver;
    private readonly WeatherDataCsvRecordMap weatherDataCsvRecordMap;
    private readonly string rootDirectory;
    private readonly DenormalizingPipeline pipeline;

    public DenormalizingPipelineTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        this.rootDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        this.fileSystem.Directory.CreateDirectory(this.rootDirectory);
        this.csvRecordWriter = new CsvRecordWriter(this.fileSystem);
        this.placeCsvFileNameResolver = new PlaceCsvFileNameResolver(this.fileSystem);
        this.weatherDataCsvRecordMap = new WeatherDataCsvRecordMap(
            new WeatherCharacteristicsEnglishCsvConverter(new WeatherCharacteristicConverter()));
        this.pipeline = new DenormalizingPipeline(
            NullLogger<DenormalizingPipeline>.Instance,
            this.fileSystem,
            this.placeCsvFileNameResolver,
            new NormalizedColumnsWeatherDataCsvReader(this.fileSystem, this.weatherDataCsvRecordMap),
            new DenormalizedWeatherDataCsvWriter(this.fileSystem, this.placeCsvFileNameResolver));
    }

    [Fact]
    public void Run_WritesDenormalizedCsvFromParsedSource()
    {
        var normalizedColumnsDirectory = this.fileSystem.Path.Combine(this.rootDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);
        var stageDirectory = this.rootDirectory;
        this.fileSystem.Directory.CreateDirectory(normalizedColumnsDirectory);

        this.WriteWeatherRecords(
            normalizedColumnsDirectory,
            "Kyiv.csv",
            new[]
            {
                new WeatherDataCsvRecord(new WeatherDataRow(
                    new DateTime(2003, 1, 1, 0, 0, 0),
                    WeatherCharacteristics.Clear,
                    -12,
                    315,
                    2.0m,
                    750,
                    70)),
            });

        this.pipeline.Run(new DenormalizingRunOptions(normalizedColumnsDirectory, stageDirectory, RunInParallel: false));

        var outputPath = this.fileSystem.Path.Combine(stageDirectory, "Kyiv.csv");
        Assert.True(this.fileSystem.File.Exists(outputPath));

        var rows = this.ReadCsv(outputPath);
        var clearColumn = WeatherCharacteristicsColumns.All
            .Single(pair => pair.Flag == WeatherCharacteristics.Clear)
            .ColumnName;

        Assert.Equal(WeatherCsvColumns.Place, rows[0][0]);
        Assert.Equal("Kyiv", rows[1][0]);
        Assert.Contains(clearColumn, rows[0]);
        Assert.Equal("1", rows[1][Array.IndexOf(rows[0], clearColumn)]);
    }

    [Fact]
    public void Run_ThrowsWhenAllPlacesHaveNoDataRows()
    {
        var normalizedColumnsDirectory = this.fileSystem.Path.Combine(this.rootDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);
        var stageDirectory = this.rootDirectory;
        this.fileSystem.Directory.CreateDirectory(normalizedColumnsDirectory);

        this.WriteWeatherRecords(
            normalizedColumnsDirectory,
            "Kyiv.csv",
            Array.Empty<WeatherDataCsvRecord>());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            this.pipeline.Run(new DenormalizingRunOptions(normalizedColumnsDirectory, stageDirectory, RunInParallel: false)));

        Assert.Contains("Denormalization produced no output files", exception.Message, StringComparison.Ordinal);
        Assert.Contains(stageDirectory, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ThrowsWhenNoOutputProduced_AndNormalizedColumnsDirectoryIsEmpty()
    {
        var normalizedColumnsDirectory = this.fileSystem.Path.Combine(this.rootDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);
        var stageDirectory = this.rootDirectory;
        this.fileSystem.Directory.CreateDirectory(normalizedColumnsDirectory);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            this.pipeline.Run(new DenormalizingRunOptions(normalizedColumnsDirectory, stageDirectory, RunInParallel: false)));

        Assert.Contains("Denormalization produced no output files", exception.Message, StringComparison.Ordinal);
        Assert.Contains(stageDirectory, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_OrdersOutputRowsByTime()
    {
        var normalizedColumnsDirectory = this.fileSystem.Path.Combine(this.rootDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);
        var stageDirectory = this.rootDirectory;
        this.fileSystem.Directory.CreateDirectory(normalizedColumnsDirectory);

        this.WriteWeatherRecords(
            normalizedColumnsDirectory,
            "Kyiv.csv",
            new[]
            {
                new WeatherDataCsvRecord(new WeatherDataRow(
                    new DateTime(2003, 1, 1, 6, 0, 0),
                    WeatherCharacteristics.Rain,
                    -5,
                    90,
                    3.5m,
                    745,
                    85)),
                new WeatherDataCsvRecord(new WeatherDataRow(
                    new DateTime(2003, 1, 1, 0, 0, 0),
                    WeatherCharacteristics.Clear,
                    -12,
                    315,
                    2.0m,
                    750,
                    70)),
            });

        this.pipeline.Run(new DenormalizingRunOptions(normalizedColumnsDirectory, stageDirectory, RunInParallel: false));

        var rows = this.ReadCsv(this.fileSystem.Path.Combine(stageDirectory, "Kyiv.csv"));
        var dateTimeIndex = Array.IndexOf(rows[0], WeatherCsvColumns.DateTime);
        Assert.Equal("2003-01-01 00:00", rows[1][dateTimeIndex]);
        Assert.Equal("2003-01-01 06:00", rows[2][dateTimeIndex]);
    }

    [Fact]
    public void Run_ThrowsWhenNormalizedColumnsDirectoryMissing()
    {
        var normalizedColumnsDirectory = this.fileSystem.Path.Combine(this.rootDirectory, "missing");
        var stageDirectory = this.fileSystem.Path.Combine(this.rootDirectory, "out");

        var exception = Assert.Throws<DirectoryNotFoundException>(() =>
            this.pipeline.Run(new DenormalizingRunOptions(normalizedColumnsDirectory, stageDirectory, RunInParallel: false)));

        Assert.Contains("Weather CSV directory not found", exception.Message, StringComparison.Ordinal);
        Assert.Contains(normalizedColumnsDirectory, exception.Message, StringComparison.Ordinal);
    }

    private int WriteWeatherRecords(string outputDirectory, string fileName, IEnumerable<WeatherDataCsvRecord> records) =>
        this.csvRecordWriter.WriteRecords(
            outputDirectory,
            fileName,
            records,
            context => context.RegisterClassMap(this.weatherDataCsvRecordMap));

    private List<string[]> ReadCsv(string csvPath)
    {
        using var reader = new StreamReader(this.fileSystem.File.OpenRead(csvPath));
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<string[]>();
        while (csv.Read())
        {
            var row = new string[csv.Parser.Count];
            for (var i = 0; i < csv.Parser.Count; i++)
            {
                row[i] = csv.GetField(i) ?? string.Empty;
            }

            rows.Add(row);
        }

        return rows;
    }
}
