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
    private readonly NormalizedColumnsWeatherDataCsvReader normalizedColumnsWeatherDataCsvReader;
    private readonly DenormalizedWeatherDataCsvWriter denormalizedWeatherDataCsvWriter;

    public DenormalizingPipeline(
        ILogger<DenormalizingPipeline> logger,
        IFileSystem fileSystem,
        PlaceCsvFileNameResolver placeCsvFileNameResolver,
        NormalizedColumnsWeatherDataCsvReader normalizedColumnsWeatherDataCsvReader,
        DenormalizedWeatherDataCsvWriter denormalizedWeatherDataCsvWriter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(placeCsvFileNameResolver);
        Argument.ThrowIfNull(normalizedColumnsWeatherDataCsvReader);
        Argument.ThrowIfNull(denormalizedWeatherDataCsvWriter);

        this.logger = logger;
        this.fileSystem = fileSystem;
        this.placeCsvFileNameResolver = placeCsvFileNameResolver;
        this.normalizedColumnsWeatherDataCsvReader = normalizedColumnsWeatherDataCsvReader;
        this.denormalizedWeatherDataCsvWriter = denormalizedWeatherDataCsvWriter;
    }

    public void Run(DenormalizingRunOptions options)
    {
        Argument.ThrowIfNull(options);
        if (options.RunInParallel)
        {
            this.logger.LogInformation(
                "Denormalizing stage start (parallel, max degree: {MaxDegree}) from {SourceDir} to {OutputDir}",
                Environment.ProcessorCount,
                options.NormalizedColumnsDirectory,
                options.StageDirectory);
        }
        else
        {
            this.logger.LogInformation(
                "Denormalizing stage start (sequential) from {SourceDir} to {OutputDir}",
                options.NormalizedColumnsDirectory,
                options.StageDirectory);
        }

        if (!this.fileSystem.Directory.Exists(options.NormalizedColumnsDirectory))
        {
            throw new DirectoryNotFoundException($"Weather CSV directory not found: {options.NormalizedColumnsDirectory}");
        }

        var rowsByPlace = this.normalizedColumnsWeatherDataCsvReader.ReadAllPlaces(options.NormalizedColumnsDirectory);

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
                    this.logger.LogDebug("Skipping denormalized CSV generation for {Place} because it has no rows.", kvp.Key);
                    return;
                }

                var csvFileName = this.placeCsvFileNameResolver.ToCsvFileName(kvp.Key);
                var rowCount = this.denormalizedWeatherDataCsvWriter.WritePlaceRows(
                    options.StageDirectory,
                    csvFileName,
                    kvp.Value.OrderBy(row => row.Time).ToList(),
                    includePlaceColumn: true);
                writtenRowCounts[kvp.Key] = rowCount;

                this.logger.LogInformation(
                    "Wrote denormalized CSV for {Place} to {CsvPath} ({RowCount} rows)",
                    kvp.Key,
                    this.fileSystem.Path.Combine(options.StageDirectory, csvFileName),
                    rowCount);
            });

        if (writtenRowCounts.Count == 0)
        {
            throw new InvalidOperationException(
                $"Denormalization produced no output files in '{options.StageDirectory}'.");
        }

        totalStopwatch.Stop();
        this.logger.LogInformation(
            "Denormalizing complete from {SourceDir} to {OutputDir} ({PlaceCount} places, {TotalRows} rows, {ElapsedSeconds:F2}s)",
            options.NormalizedColumnsDirectory,
            options.StageDirectory,
            writtenRowCounts.Count,
            writtenRowCounts.Values.Sum(),
            totalStopwatch.Elapsed.TotalSeconds);
    }
}
