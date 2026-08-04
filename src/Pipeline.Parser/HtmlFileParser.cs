// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using System.Collections.Concurrent;
using System.Diagnostics;
using Common;
using Microsoft.Extensions.Logging;
using Pipeline.Core.Execution;
using Pipeline.SourceFileSystem;

public sealed class HtmlFileParser
{
    private readonly ILogger<HtmlFileParser> logger;

    public HtmlFileParser(ILogger<HtmlFileParser> logger)
    {
        Argument.ThrowIfNull(logger);

        this.logger = logger;
    }

    public List<(string FilePath, HtmlParseResult Result)> ParseFiles(
        ISourceFileSystem source,
        RealWeatherHtmlParser htmlParser,
        ParsingIssueCollector issueCollector,
        bool runInParallel,
        out int parsingSuccessfulCount,
        out int parsingUnsuccessfulCount,
        out long totalFileProcessingTime)
    {
        Argument.ThrowIfNull(source);
        Argument.ThrowIfNull(htmlParser);
        Argument.ThrowIfNull(issueCollector);

        if (runInParallel && !source.SupportsParallel)
        {
            throw new InvalidOperationException(
                "7z source archives do not support parallel parsing; set RunInParallel to false.");
        }

        if (runInParallel)
        {
            return this.ParseFilesParallel(
                source,
                htmlParser,
                issueCollector,
                out parsingSuccessfulCount,
                out parsingUnsuccessfulCount,
                out totalFileProcessingTime);
        }

        return this.ParseFilesSequential(
            source,
            htmlParser,
            issueCollector,
            out parsingSuccessfulCount,
            out parsingUnsuccessfulCount,
            out totalFileProcessingTime);
    }

    private List<(string FilePath, HtmlParseResult Result)> ParseFilesSequential(
        ISourceFileSystem source,
        RealWeatherHtmlParser htmlParser,
        ParsingIssueCollector issueCollector,
        out int parsingSuccessfulCount,
        out int parsingUnsuccessfulCount,
        out long totalFileProcessingTime)
    {
        var rawParseResultsWithPaths = new List<(string FilePath, HtmlParseResult Result)>();
        var successfulCount = 0;
        var unsuccessfulCount = 0;
        long totalProcessingTime = 0;

        foreach (var file in source.OpenAll())
        {
            using (file)
            {
                var outcome = this.TryParseFile(file, htmlParser, issueCollector);
                totalProcessingTime += outcome.ElapsedMilliseconds;
                if (outcome.Result is not null)
                {
                    rawParseResultsWithPaths.Add((file.Path, outcome.Result));
                    successfulCount++;
                }
                else
                {
                    unsuccessfulCount++;
                }
            }
        }

        parsingSuccessfulCount = successfulCount;
        parsingUnsuccessfulCount = unsuccessfulCount;
        totalFileProcessingTime = totalProcessingTime;
        return rawParseResultsWithPaths;
    }

    private List<(string FilePath, HtmlParseResult Result)> ParseFilesParallel(
        ISourceFileSystem source,
        RealWeatherHtmlParser htmlParser,
        ParsingIssueCollector issueCollector,
        out int parsingSuccessfulCount,
        out int parsingUnsuccessfulCount,
        out long totalFileProcessingTime)
    {
        // Materialize SourceFile handles first (lazy Content — no opens yet for directory sources).
        var files = source.OpenAll().ToList();
        var rawParseResultsWithPaths = new ConcurrentBag<(string FilePath, HtmlParseResult Result)>();
        var successfulCountLocal = 0;
        var unsuccessfulCountLocal = 0;
        long totalProcessingTimeLocal = 0;
        var maxDegree = ParallelExecutionOptions.GetMaxDegreeOfParallelism(runInParallel: true);

        try
        {
            Parallel.ForEach(
                files,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegree },
                file =>
                {
                    using (file)
                    {
                        var outcome = this.TryParseFile(file, htmlParser, issueCollector);
                        Interlocked.Add(ref totalProcessingTimeLocal, outcome.ElapsedMilliseconds);
                        if (outcome.Result is not null)
                        {
                            rawParseResultsWithPaths.Add((file.Path, outcome.Result));
                            Interlocked.Increment(ref successfulCountLocal);
                        }
                        else
                        {
                            Interlocked.Increment(ref unsuccessfulCountLocal);
                        }
                    }
                });
        }
        finally
        {
            // Dispose any handles Parallel.ForEach did not run (e.g. if enumeration/setup failed).
            foreach (var file in files)
            {
                file.Dispose();
            }
        }

        parsingSuccessfulCount = successfulCountLocal;
        parsingUnsuccessfulCount = unsuccessfulCountLocal;
        totalFileProcessingTime = totalProcessingTimeLocal;
        return rawParseResultsWithPaths.ToList();
    }

    private FileParseOutcome TryParseFile(
        SourceFile file,
        RealWeatherHtmlParser htmlParser,
        ParsingIssueCollector issueCollector)
    {
        this.logger.LogTrace("File found: {FilePath}", file.Path);

        var fileStopwatch = Stopwatch.StartNew();
        try
        {
            var result = htmlParser.Parse(file.Content, file.Path);
            this.logger.LogDebug("Successfully parsed HTML file: {FilePath}", file.Path);
            return new FileParseOutcome(result, fileStopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            issueCollector.AddParseFailure(file.Path);
            this.logger.LogError(ex, "Failed to parse HTML file: {FilePath}", file.Path);
            return new FileParseOutcome(null, fileStopwatch.ElapsedMilliseconds);
        }
        finally
        {
            fileStopwatch.Stop();
        }
    }

    private sealed record FileParseOutcome(HtmlParseResult? Result, long ElapsedMilliseconds);
}
