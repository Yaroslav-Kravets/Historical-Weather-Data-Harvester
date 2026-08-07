// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Runner;

using System.IO.Abstractions;
using Common;
using HtmlLog;
using HtmlLogCsvComparer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pipeline.Analysis;
using Pipeline.Denormalizer;
using Pipeline.Parser;
using Pipeline.Runner.Logging;
using Pipeline.Runner.Settings;
using Pipeline.TimeNormalizer;

public sealed class PipelineRunner
{
    private readonly IConfiguration configuration;
    private readonly IFileSystem fileSystem;
    private readonly RunnerSettings settings;

    public PipelineRunner(IConfiguration configuration, IFileSystem fileSystem, RunnerSettings settings)
    {
        Argument.ThrowIfNull(configuration);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(settings);

        this.configuration = configuration;
        this.fileSystem = fileSystem;
        this.settings = settings;
    }

    public void Run()
    {
        var runTimestamp = DateTime.Now;
        var logDateTime = runTimestamp.ToString(HtmlLogRunDirectory.TimestampFormat);
        var runDirectory = HtmlLogRunDirectory.FormatDirectoryName(runTimestamp);
        var parsedStageDirectory = this.fileSystem.Path.Combine(runDirectory, WeatherCsvOutputPaths.ParsedStageDirectoryName);
        var timeNormalizedStageDirectory = this.fileSystem.Path.Combine(
            runDirectory,
            WeatherCsvOutputPaths.TimeNormalizedStageDirectoryName);

        if (string.IsNullOrWhiteSpace(this.settings.HistoricalWeatherFilesRoot))
        {
            using (var errorStage = StageServiceProviderFactory.Create(
                this.configuration,
                this.fileSystem,
                parsedStageDirectory,
                this.fileSystem.Path.Combine(parsedStageDirectory, $"log{logDateTime}.log"),
                services => services.AddParserServices()))
            {
                var logger = errorStage.ServiceProvider.GetRequiredService<ILogger<PipelineRunner>>();
                logger.LogError("HistoricalWeatherFilesRoot is not configured in appsettings.json");
            }

            throw new InvalidOperationException("HistoricalWeatherFilesRoot is not configured in appsettings.json");
        }

        using (var parsedStage = StageServiceProviderFactory.Create(
            this.configuration,
            this.fileSystem,
            parsedStageDirectory,
            this.fileSystem.Path.Combine(parsedStageDirectory, $"log{logDateTime}.log"),
            services =>
            {
                services.AddParserServices();
                if (this.settings.RunAnalysis)
                {
                    services.AddAnalysisServices();
                }
            }))
        {
            var logger = parsedStage.ServiceProvider.GetRequiredService<ILogger<PipelineRunner>>();
            logger.LogInformation("Start");

            var parsingPipeline = parsedStage.ServiceProvider.GetRequiredService<ParsingPipeline>();
            var htmlLogFileManager = parsedStage.ServiceProvider.GetRequiredService<HtmlLogFileManager>();
            var parsedHtmlReportPath = this.fileSystem.Path.Combine(
                parsedStageDirectory,
                $"result{logDateTime}.html");

            using (var htmlWriter = new HtmlLogWriter(
                htmlLogFileManager,
                parsedHtmlReportPath,
                "Historical Weather Data Harvester — Parsing"))
            {
                parsingPipeline.Run(new ParsingRunOptions(
                    this.settings.HistoricalWeatherFilesRoot,
                    parsedStageDirectory,
                    htmlWriter,
                    this.settings.RunInParallel));

                if (this.settings.RunAnalysis)
                {
                    parsedStage.ServiceProvider
                        .GetRequiredService<AnalysisPipeline>()
                        .AnalyzeStage(new AnalysisRunOptions(
                            parsedStageDirectory,
                            htmlWriter,
                            Required: true));
                }
            }
        }

        using (var denormStage = StageServiceProviderFactory.Create(
            this.configuration,
            this.fileSystem,
            parsedStageDirectory,
            this.fileSystem.Path.Combine(parsedStageDirectory, $"log-denorm{logDateTime}.log"),
            services => services.AddDenormalizerServices()))
        {
            var denormalizingPipeline = denormStage.ServiceProvider.GetRequiredService<DenormalizingPipeline>();
            denormalizingPipeline.Run(new DenormalizingRunOptions(
                this.fileSystem.Path.Combine(parsedStageDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName),
                parsedStageDirectory,
                this.settings.RunInParallel));
        }

        if (this.settings.RunTimeNormalization)
        {
            using (var timeNormalizerStage = StageServiceProviderFactory.Create(
                this.configuration,
                this.fileSystem,
                timeNormalizedStageDirectory,
                this.fileSystem.Path.Combine(timeNormalizedStageDirectory, $"log{logDateTime}.log"),
                services =>
                {
                    services.AddTimeNormalizerServices();
                    if (this.settings.RunAnalysis)
                    {
                        services.AddAnalysisServices();
                    }
                }))
            {
                var timeNormalizingPipeline = timeNormalizerStage.ServiceProvider
                    .GetRequiredService<TimeNormalizingPipeline>();
                var htmlLogFileManager = timeNormalizerStage.ServiceProvider
                    .GetRequiredService<HtmlLogFileManager>();
                var timeNormalizedHtmlReportPath = this.fileSystem.Path.Combine(
                    timeNormalizedStageDirectory,
                    $"result{logDateTime}.html");

                using (var htmlWriter = new HtmlLogWriter(
                    htmlLogFileManager,
                    timeNormalizedHtmlReportPath,
                    "Historical Weather Data Harvester — Time Normalizing"))
                {
                    timeNormalizingPipeline.Run(new TimeNormalizingRunOptions(
                        parsedStageDirectory,
                        timeNormalizedStageDirectory,
                        htmlWriter,
                        this.settings.RunInParallel));

                    if (this.settings.RunAnalysis)
                    {
                        timeNormalizerStage.ServiceProvider
                            .GetRequiredService<AnalysisPipeline>()
                            .AnalyzeStage(new AnalysisRunOptions(
                                timeNormalizedStageDirectory,
                                htmlWriter,
                                Required: false));
                    }
                }
            }
        }

        if (this.settings.RunHtmlLogCsvComparison)
        {
            using (var comparisonStage = StageServiceProviderFactory.Create(
                this.configuration,
                this.fileSystem,
                parsedStageDirectory,
                this.fileSystem.Path.Combine(parsedStageDirectory, $"log-compare{logDateTime}.log"),
                services => services.AddHtmlLogCsvComparerServices()))
            {
                var logger = comparisonStage.ServiceProvider.GetRequiredService<ILogger<PipelineRunner>>();
                var comparer = comparisonStage.ServiceProvider.GetRequiredService<CsvComparisonOutput>();
                var searchRoot = this.fileSystem.Directory.GetCurrentDirectory();
                var exitCode = comparer.CompareChain(searchRoot);

                switch (exitCode)
                {
                    case 0:
                        logger.LogInformation("HtmlLog CSV chain comparison finished; all pairs equal.");
                        break;
                    case 1:
                        logger.LogWarning(
                            "HtmlLog CSV chain comparison finished; some pairs not equal — see comparison log above.");
                        break;
                    default:
                        logger.LogWarning(
                            "HtmlLog CSV chain comparison finished with errors — see comparison log/SUMMARY above " +
                            "(SUMMARY includes equal, not-equal, and error counts).");
                        break;
                }
            }
        }

        using (var finishStage = StageServiceProviderFactory.Create(
            this.configuration,
            this.fileSystem,
            parsedStageDirectory,
            this.fileSystem.Path.Combine(parsedStageDirectory, $"log{logDateTime}.log"),
            _ => { }))
        {
            finishStage.ServiceProvider
                .GetRequiredService<ILogger<PipelineRunner>>()
                .LogInformation("Finish");
        }
    }
}
