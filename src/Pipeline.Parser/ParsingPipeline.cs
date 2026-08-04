// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using System.Diagnostics;
using System.IO.Abstractions;
using Common;
using HtmlLog;
using Microsoft.Extensions.Logging;
using Pipeline.SourceFileSystem;

public sealed class ParsingPipeline
{
    private readonly ILogger<ParsingPipeline> logger;
    private readonly IFileSystem fileSystem;
    private readonly RealWeatherHtmlParser htmlParser;
    private readonly HtmlFileParser htmlFileParser;
    private readonly ParseResultOrganizer parseResultOrganizer;
    private readonly ParsedFileInfoFlattener parsedFileInfoFlattener;
    private readonly ParsedWeatherCharacteristicsCollector parsedWeatherCharacteristicsCollector;
    private readonly NormalizedColumnsWeatherDataCsvWriter normalizedColumnsWeatherDataCsvWriter;
    private readonly ParsedStageManifestCsvWriter parsedStageManifestCsvWriter;
    private readonly ParsedSourceFilesManifestWriter parsedSourceFilesManifestWriter;
    private readonly ParsingReportWriter parsingReportWriter;
    private readonly HtmlLogFileManager htmlLogFileManager;
    private readonly PlaceConverter placeConverter;

    public ParsingPipeline(
        ILogger<ParsingPipeline> logger,
        IFileSystem fileSystem,
        RealWeatherHtmlParser htmlParser,
        HtmlFileParser htmlFileParser,
        ParseResultOrganizer parseResultOrganizer,
        ParsedFileInfoFlattener parsedFileInfoFlattener,
        ParsedWeatherCharacteristicsCollector parsedWeatherCharacteristicsCollector,
        NormalizedColumnsWeatherDataCsvWriter normalizedColumnsWeatherDataCsvWriter,
        ParsedStageManifestCsvWriter parsedStageManifestCsvWriter,
        ParsedSourceFilesManifestWriter parsedSourceFilesManifestWriter,
        ParsingReportWriter parsingReportWriter,
        HtmlLogFileManager htmlLogFileManager,
        PlaceConverter placeConverter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(htmlParser);
        Argument.ThrowIfNull(htmlFileParser);
        Argument.ThrowIfNull(parseResultOrganizer);
        Argument.ThrowIfNull(parsedFileInfoFlattener);
        Argument.ThrowIfNull(parsedWeatherCharacteristicsCollector);
        Argument.ThrowIfNull(normalizedColumnsWeatherDataCsvWriter);
        Argument.ThrowIfNull(parsedStageManifestCsvWriter);
        Argument.ThrowIfNull(parsedSourceFilesManifestWriter);
        Argument.ThrowIfNull(parsingReportWriter);
        Argument.ThrowIfNull(htmlLogFileManager);
        Argument.ThrowIfNull(placeConverter);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.htmlParser = htmlParser;
        this.htmlFileParser = htmlFileParser;
        this.parseResultOrganizer = parseResultOrganizer;
        this.parsedFileInfoFlattener = parsedFileInfoFlattener;
        this.parsedWeatherCharacteristicsCollector = parsedWeatherCharacteristicsCollector;
        this.normalizedColumnsWeatherDataCsvWriter = normalizedColumnsWeatherDataCsvWriter;
        this.parsedStageManifestCsvWriter = parsedStageManifestCsvWriter;
        this.parsedSourceFilesManifestWriter = parsedSourceFilesManifestWriter;
        this.parsingReportWriter = parsingReportWriter;
        this.htmlLogFileManager = htmlLogFileManager;
        this.placeConverter = placeConverter;
    }

    public void Run(ParsingRunOptions options)
    {
        Argument.ThrowIfNull(options);

        using var source = SourceFileSystemFactory.Open(this.fileSystem, options.SourceDirectory);
        var isSevenZipSource = source is SevenZipSourceFileSystem;
        if (options.RunInParallel && !source.SupportsParallel)
        {
            throw new InvalidOperationException(
                "7z source archives do not support parallel parsing; set RunInParallel to false.");
        }

        if (options.RunInParallel)
        {
            this.logger.LogInformation(
                "Parsing stage start (parallel, max degree: {MaxDegree})",
                Environment.ProcessorCount);
        }
        else if (isSevenZipSource)
        {
            this.logger.LogInformation("Parsing stage start (sequential 7z archive)");
        }
        else
        {
            this.logger.LogInformation("Parsing stage start (sequential)");
        }

        var issueCollector = new ParsingIssueCollector(this.placeConverter);
        var totalStopwatch = Stopwatch.StartNew();

        this.logger.LogDebug("Reading files from: {RootPath}", options.SourceDirectory);

        var sourceFileCount = source.GetFiles().Count;
        var rawParseResultsWithPaths = this.htmlFileParser.ParseFiles(
            source,
            this.htmlParser,
            issueCollector,
            options.RunInParallel,
            out var parsingSuccessfulCount,
            out var parsingUnsuccessfulCount,
            out var totalFileProcessingTime);

        var organizationResult = this.parseResultOrganizer.OrganizeByPlaceAndDate(rawParseResultsWithPaths, issueCollector);
        parsingSuccessfulCount -= organizationResult.PathPlaceMismatchRejections;
        parsingUnsuccessfulCount += organizationResult.PathPlaceMismatchRejections;
        var flattenedRawParseResults = this.parsedFileInfoFlattener.Flatten(organizationResult.ResultsByPlace).ToList();

        totalStopwatch.Stop();

        var totalTime = totalStopwatch.Elapsed.TotalSeconds;
        var averageTime = sourceFileCount > 0 ? (totalFileProcessingTime / (double)sourceFileCount) / 1000.0 : 0;

        var normalizedColumnsDir = this.fileSystem.Path.Combine(options.ParsedStageDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName);
        var parsedCharacteristics = this.parsedWeatherCharacteristicsCollector.Collect(organizationResult.ResultsByPlace);

        var projected = organizationResult.ResultsByPlace.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<WeatherDataRow>)ProjectRows(kvp.Value).ToList(),
            StringComparer.OrdinalIgnoreCase);
        this.normalizedColumnsWeatherDataCsvWriter.WritePlaceRows(projected, normalizedColumnsDir, "parsed");
        this.parsedStageManifestCsvWriter.WriteParsedPlacesManifest(organizationResult.ParsedPlaces, options.ParsedStageDirectory);
        this.parsedStageManifestCsvWriter.WriteWeatherCharacteristicsManifest(parsedCharacteristics, options.ParsedStageDirectory);
        this.parsedSourceFilesManifestWriter.Write(organizationResult.SourceFileEntries, options.ParsedStageDirectory);

        using (var htmlWriter = new HtmlLogWriter(this.htmlLogFileManager, options.HtmlReportPath, "Historical Weather Data Harvester — Parsing"))
        {
            this.parsingReportWriter.WriteReport(
                htmlWriter,
                options.SourceDirectory,
                isSevenZipSource,
                sourceFileCount,
                parsingSuccessfulCount,
                parsingUnsuccessfulCount,
                totalTime,
                averageTime,
                organizationResult.ResultsByPlace,
                issueCollector,
                flattenedRawParseResults);
        }

        PlacePathSelfCheckLogger.LogRunSummary(this.logger, issueCollector);
        this.logger.LogInformation("Parsing stage complete");
    }

    private static IEnumerable<WeatherDataRow> ProjectRows(SortedDictionary<DateTime, ParsedDateEntry> resultsByDate)
    {
        foreach (var (_, entry) in resultsByDate)
        {
            foreach (var row in entry.Result.WeatherDataRows.OrderBy(row => row.Time))
            {
                yield return row;
            }
        }
    }
}
