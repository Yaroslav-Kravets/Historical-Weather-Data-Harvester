// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer.Tests;

using System.IO.Abstractions;
using FileSystem.TestSupport;
using HtmlLog;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class TimeNormalizingPipelineTests
{
    private readonly IFileSystem fileSystem;
    private readonly DenormalizedWeatherDataCsvWriter denormalizedWeatherDataCsvWriter;
    private readonly string parsedStageDirectory;
    private readonly string timeNormalizedStageDirectory;
    private readonly TimeNormalizingPipeline timeNormalizingPipeline;

    public TimeNormalizingPipelineTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        var baseDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        this.parsedStageDirectory = this.fileSystem.Path.Combine(baseDirectory, WeatherCsvOutputPaths.ParsedStageDirectoryName);
        this.timeNormalizedStageDirectory = this.fileSystem.Path.Combine(
            baseDirectory,
            WeatherCsvOutputPaths.TimeNormalizedStageDirectoryName);
        this.fileSystem.Directory.CreateDirectory(this.parsedStageDirectory);
        this.fileSystem.Directory.CreateDirectory(this.timeNormalizedStageDirectory);
        this.denormalizedWeatherDataCsvWriter = new DenormalizedWeatherDataCsvWriter(
            this.fileSystem,
            new PlaceCsvFileNameResolver(this.fileSystem));
        this.timeNormalizingPipeline = CreatePipeline(this.fileSystem);
    }

    [Fact]
    public void Run_ThrowsDirectoryNotFoundException_WhenSourceStageDirectoryDoesNotExist()
    {
        var missingSourceDirectory = InMemoryFileSystem.UnderRoot(this.fileSystem, Guid.NewGuid().ToString("N"));
        var htmlReportPath = this.fileSystem.Path.Combine(this.timeNormalizedStageDirectory, "result.html");

        var exception = Assert.Throws<DirectoryNotFoundException>(() =>
            this.RunTimeNormalizing(missingSourceDirectory, htmlReportPath, runInParallel: false));

        Assert.Contains("Parsed stage directory not found", exception.Message, StringComparison.Ordinal);
        Assert.Contains(missingSourceDirectory, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ThrowsInvalidOperationException_WhenNoDenormalizedPlaceCsvs()
    {
        var htmlReportPath = this.fileSystem.Path.Combine(this.timeNormalizedStageDirectory, "result.html");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            this.RunTimeNormalizing(this.parsedStageDirectory, htmlReportPath, runInParallel: false));

        Assert.Contains("No denormalized place CSVs found", exception.Message, StringComparison.Ordinal);
        Assert.Contains(this.parsedStageDirectory, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WritesNormalizedAndDenormalizedOutputs()
    {
        var archiveDate = new DateTime(2003, 1, 1);
        this.denormalizedWeatherDataCsvWriter.WritePlaceRows(
            this.parsedStageDirectory,
            "Kyiv.csv",
            CreateFullDayRows(archiveDate));

        var htmlReportPath = this.fileSystem.Path.Combine(this.timeNormalizedStageDirectory, "result.html");
        this.RunTimeNormalizing(this.parsedStageDirectory, htmlReportPath, runInParallel: false);

        var normalizedPath = this.fileSystem.Path.Combine(
            this.timeNormalizedStageDirectory,
            WeatherCsvOutputPaths.NormalizedColumnsDirectoryName,
            "Kyiv.csv");
        var normalizedDenormalizedPath = this.fileSystem.Path.Combine(
            this.timeNormalizedStageDirectory,
            "Kyiv.csv");

        Assert.True(this.fileSystem.File.Exists(normalizedPath));
        Assert.True(this.fileSystem.File.Exists(normalizedDenormalizedPath));
        Assert.True(this.fileSystem.File.Exists(htmlReportPath));

        var normalizedHeader = this.fileSystem.File.ReadLines(normalizedPath).First();
        Assert.DoesNotContain(WeatherCsvColumns.Place, normalizedHeader);

        var denormalizedHeader = this.fileSystem.File.ReadLines(normalizedDenormalizedPath).First();
        Assert.Equal(WeatherCsvColumns.Place, denormalizedHeader.Split(',')[0]);
    }

    [Fact]
    public void Run_ReadsParsedStageCsvWithPlaceColumn()
    {
        var archiveDate = new DateTime(2003, 1, 1);
        this.denormalizedWeatherDataCsvWriter.WritePlaceRows(
            this.parsedStageDirectory,
            "Kyiv.csv",
            CreateFullDayRows(archiveDate),
            includePlaceColumn: true);

        var htmlReportPath = this.fileSystem.Path.Combine(this.timeNormalizedStageDirectory, "result-with-place.html");
        this.RunTimeNormalizing(this.parsedStageDirectory, htmlReportPath, runInParallel: false);

        var normalizedDenormalizedPath = this.fileSystem.Path.Combine(this.timeNormalizedStageDirectory, "Kyiv.csv");
        Assert.True(this.fileSystem.File.Exists(normalizedDenormalizedPath));
        Assert.Equal(
            WeatherCsvColumns.Place,
            this.fileSystem.File.ReadLines(normalizedDenormalizedPath).First().Split(',')[0]);
    }

    private static WeatherDataRow[] CreateFullDayRows(DateTime archiveDate) =>
        new[]
        {
            new WeatherDataRow(archiveDate.AddHours(0), WeatherCharacteristics.Clear, -12, 315, 2.0m, 750, 70),
            new WeatherDataRow(archiveDate.AddHours(3), WeatherCharacteristics.Clear, -13, 315, 2.0m, 750, 70),
            new WeatherDataRow(archiveDate.AddHours(6), WeatherCharacteristics.Clear, -11, 315, 2.0m, 750, 70),
            new WeatherDataRow(archiveDate.AddHours(9), WeatherCharacteristics.Clear, -10, 315, 2.0m, 750, 70),
            new WeatherDataRow(archiveDate.AddHours(12), WeatherCharacteristics.Clear, -9, 315, 2.0m, 750, 70),
            new WeatherDataRow(archiveDate.AddHours(15), WeatherCharacteristics.Clear, -8, 315, 2.0m, 750, 70),
            new WeatherDataRow(archiveDate.AddHours(18), WeatherCharacteristics.Clear, -7, 315, 2.0m, 750, 70),
            new WeatherDataRow(archiveDate.AddHours(21), WeatherCharacteristics.Clear, -6, 315, 2.0m, 750, 70),
        };

    private static TimeNormalizingPipeline CreatePipeline(IFileSystem fileSystem)
    {
        var placeCsvFileNameResolver = new PlaceCsvFileNameResolver(fileSystem);
        var csvRecordWriter = new CsvRecordWriter(fileSystem);
        var denormalizedWeatherDataCsvReader = new DenormalizedWeatherDataCsvReader(fileSystem);
        var denormalizedWeatherDataCsvWriter = new DenormalizedWeatherDataCsvWriter(fileSystem, placeCsvFileNameResolver);
        var parsedSourceFilesManifestReader = new ParsedSourceFilesManifestReader(fileSystem);

        return new TimeNormalizingPipeline(
            NullLogger<TimeNormalizingPipeline>.Instance,
            fileSystem,
            placeCsvFileNameResolver,
            denormalizedWeatherDataCsvReader,
            denormalizedWeatherDataCsvWriter,
            parsedSourceFilesManifestReader,
            new PlaceTimeNormalizer(
                NullLogger<PlaceTimeNormalizer>.Instance,
                new ObservationTimeNormalizer(NullLogger<ObservationTimeNormalizer>.Instance),
                new ObservationTimeInterpolator(NullLogger<ObservationTimeInterpolator>.Instance),
                parsedSourceFilesManifestReader),
            new NormalizedColumnsWeatherDataCsvWriter(
                NullLogger<NormalizedColumnsWeatherDataCsvWriter>.Instance,
                fileSystem,
                csvRecordWriter,
                placeCsvFileNameResolver,
                new WeatherDataCsvRecordMap(
                    new WeatherCharacteristicsEnglishCsvConverter(new WeatherCharacteristicConverter()))),
            new TimeNormalizingReportWriter(
                new TimeNormalizingPlaceErrorCountsBuilder(),
                denormalizedWeatherDataCsvReader,
                placeCsvFileNameResolver,
                fileSystem));
    }

    private void RunTimeNormalizing(string parsedStageDirectory, string htmlReportPath, bool runInParallel)
    {
        using var fileManager = new HtmlLogFileManager(this.fileSystem);
        using var htmlWriter = new HtmlLogWriter(fileManager, htmlReportPath, "Time Normalizing");
        this.timeNormalizingPipeline.Run(new TimeNormalizingRunOptions(
            parsedStageDirectory,
            this.timeNormalizedStageDirectory,
            htmlWriter,
            runInParallel));
    }
}
