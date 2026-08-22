// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Denormalizer;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Abstractions;
using Common;
using Microsoft.Extensions.Logging;

public sealed class DenormalizingPipeline
{
    private readonly ILogger<DenormalizingPipeline> logger;
    private readonly IFileSystem fileSystem;
    private readonly PlaceCsvFileNameResolver placeCsvFileNameResolver;
    private readonly NarrowFormatWeatherDataCsvReader narrowFormatWeatherDataCsvReader;
    private readonly WideFormatWeatherDataCsvWriter wideFormatWeatherDataCsvWriter;

    public DenormalizingPipeline(
        ILogger<DenormalizingPipeline> logger,
        IFileSystem fileSystem,
        PlaceCsvFileNameResolver placeCsvFileNameResolver,
        NarrowFormatWeatherDataCsvReader narrowFormatWeatherDataCsvReader,
        WideFormatWeatherDataCsvWriter wideFormatWeatherDataCsvWriter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(placeCsvFileNameResolver);
        Argument.ThrowIfNull(narrowFormatWeatherDataCsvReader);
        Argument.ThrowIfNull(wideFormatWeatherDataCsvWriter);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.placeCsvFileNameResolver = placeCsvFileNameResolver;
        this.narrowFormatWeatherDataCsvReader = narrowFormatWeatherDataCsvReader;
        this.wideFormatWeatherDataCsvWriter = wideFormatWeatherDataCsvWriter;
    }

    public void Run(DenormalizingRunOptions options)
    {
        Argument.ThrowIfNull(options);

        this.logger.LogInformation("Start");

        if (options.RunInParallel)
        {
            this.logger.LogInformation(
                "Denormalizing mode: parallel (max degree: {MaxDegree}) from {SourceDir} to {OutputDir}",
                Environment.ProcessorCount,
                options.NarrowFormatDirectory,
                options.WideFormatDirectory);
        }
        else
        {
            this.logger.LogInformation(
                "Denormalizing mode: sequential from {SourceDir} to {OutputDir}",
                options.NarrowFormatDirectory,
                options.WideFormatDirectory);
        }

        if (!this.fileSystem.Directory.Exists(options.NarrowFormatDirectory))
        {
            throw new DirectoryNotFoundException($"Weather CSV directory not found: {options.NarrowFormatDirectory}");
        }

        var rowsByPlace = this.narrowFormatWeatherDataCsvReader.ReadAllPlaces(options.NarrowFormatDirectory);

        var maxDegree = ParallelExecutionOptions.GetMaxDegreeOfParallelism(options.RunInParallel);
        var totalStopwatch = Stopwatch.StartNew();
        var writtenRowCounts = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        Parallel.ForEach(
            rowsByPlace,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegree },
            kvp =>
            {
                if (kvp.Value.Count == 0)
                {
                    this.logger.LogDebug("Skipping wide-format CSV generation for {Place} because it has no rows.", kvp.Key);
                    return;
                }

                var csvFileName = this.placeCsvFileNameResolver.ToCsvFileName(kvp.Key);
                var rowCount = this.wideFormatWeatherDataCsvWriter.WritePlaceRows(
                    options.WideFormatDirectory,
                    csvFileName,
                    kvp.Value.OrderBy(row => row.Time).ToList(),
                    includePlaceColumn: true);
                writtenRowCounts[kvp.Key] = rowCount;

                this.logger.LogInformation(
                    "Wrote wide-format CSV for {Place} to {CsvPath} ({RowCount} rows)",
                    kvp.Key,
                    this.fileSystem.Path.Combine(options.WideFormatDirectory, csvFileName),
                    rowCount);
            });

        if (writtenRowCounts.Count == 0)
        {
            throw new InvalidOperationException(
                $"Denormalization produced no output files in '{options.WideFormatDirectory}'.");
        }

        totalStopwatch.Stop();
        this.logger.LogInformation(
            "Wrote wide format from {SourceDir} to {OutputDir} ({PlaceCount} places, {TotalRows} rows, {ElapsedSeconds:F2}s)",
            options.NarrowFormatDirectory,
            options.WideFormatDirectory,
            writtenRowCounts.Count,
            writtenRowCounts.Values.Sum(),
            totalStopwatch.Elapsed.TotalSeconds);
        this.logger.LogInformation("Finish");
    }
}
