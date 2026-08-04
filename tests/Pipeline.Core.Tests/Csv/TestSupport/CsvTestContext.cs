// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Csv.TestSupport;

using System.IO.Abstractions;
using FileSystem.TestSupport;

internal sealed class CsvTestContext
{
    public CsvTestContext()
    {
        this.FileSystem = InMemoryFileSystem.Create();
        this.RootDirectory = InMemoryFileSystem.UnderRoot(this.FileSystem, Guid.NewGuid().ToString("N"));
        this.FileSystem.Directory.CreateDirectory(this.RootDirectory);
        this.CsvRecordWriter = new CsvRecordWriter(this.FileSystem);
        this.PlaceCsvFileNameResolver = new PlaceCsvFileNameResolver(this.FileSystem);
        var weatherCharacteristicConverter = new WeatherCharacteristicConverter();
        this.WeatherDataCsvRecordMap = new WeatherDataCsvRecordMap(
            new WeatherCharacteristicsEnglishCsvConverter(weatherCharacteristicConverter));
    }

    public IFileSystem FileSystem { get; }

    public string RootDirectory { get; }

    public CsvRecordWriter CsvRecordWriter { get; }

    public PlaceCsvFileNameResolver PlaceCsvFileNameResolver { get; }

    public WeatherDataCsvRecordMap WeatherDataCsvRecordMap { get; }

    public string PathUnderRoot(params string[] segments)
    {
        var allSegments = new[] { this.RootDirectory }.Concat(segments).ToArray();
        return this.FileSystem.Path.Combine(allSegments);
    }

    public string EnsureDirectoryUnderRoot(params string[] segments)
    {
        var directory = this.PathUnderRoot(segments);
        this.FileSystem.Directory.CreateDirectory(directory);
        return directory;
    }

    public int WriteWeatherRecords(
        string outputDirectory,
        string fileName,
        IEnumerable<WeatherDataCsvRecord> records) =>
        this.CsvRecordWriter.WriteRecords(
            outputDirectory,
            fileName,
            records,
            context => context.RegisterClassMap(this.WeatherDataCsvRecordMap));
}
