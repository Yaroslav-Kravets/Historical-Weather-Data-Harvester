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
        var parsedTextLogFilePath = this.StageTextLogPath(parsedStageDirectory, logDateTime);

        using (var parsedServices = this.CreateParsedStageServices(parsedStageDirectory, parsedTextLogFilePath))
        {
            if (string.IsNullOrWhiteSpace(this.settings.HistoricalWeatherFilesRoot))
            {
                parsedServices.ServiceProvider
                    .GetRequiredService<ILogger<PipelineRunner>>()
                    .LogError("HistoricalWeatherFilesRoot is not configured in appsettings.json");

                throw new InvalidOperationException("HistoricalWeatherFilesRoot is not configured in appsettings.json");
            }

            this.RunParsingStage(parsedServices.ServiceProvider, parsedStageDirectory, logDateTime);

            if (this.settings.RunAnalysis)
            {
                this.RunAnalysisStage(
                    parsedServices.ServiceProvider,
                    parsedStageDirectory,
                    logDateTime);
            }

            this.RunDenormalizationStage(parsedServices.ServiceProvider, parsedStageDirectory);

            if (this.settings.RunTimeNormalization)
            {
                var timeNormalizedTextLogFilePath = this.StageTextLogPath(
                    timeNormalizedStageDirectory,
                    logDateTime);
                using var timeNormalizedServices = this.CreateTimeNormalizedStageServices(
                    timeNormalizedStageDirectory,
                    timeNormalizedTextLogFilePath);

                this.RunTimeNormalizationStage(
                    timeNormalizedServices.ServiceProvider,
                    parsedStageDirectory,
                    timeNormalizedStageDirectory,
                    logDateTime);

                if (this.settings.RunAnalysis)
                {
                    this.RunAnalysisStage(
                        timeNormalizedServices.ServiceProvider,
                        timeNormalizedStageDirectory,
                        logDateTime);
                }
            }

            if (this.settings.RunHtmlLogCsvComparison)
            {
                this.RunHtmlLogCsvComparisonStage(parsedServices.ServiceProvider);
            }
        }
    }

    private void RunParsingStage(
        IServiceProvider serviceProvider,
        string parsedStageDirectory,
        string logDateTime) =>
        serviceProvider
            .GetRequiredService<ParsingPipeline>()
            .Run(new ParsingRunOptions(
                this.settings.HistoricalWeatherFilesRoot,
                parsedStageDirectory,
                this.StageHtmlReportPath(parsedStageDirectory, "result", logDateTime),
                this.settings.RunInParallel));

    private void RunAnalysisStage(
        IServiceProvider serviceProvider,
        string stageDirectory,
        string logDateTime) =>
        serviceProvider
            .GetRequiredService<AnalysisPipeline>()
            .AnalyzeStage(new AnalysisRunOptions(
                stageDirectory,
                this.StageHtmlReportPath(stageDirectory, "result-analysis", logDateTime)));

    private void RunDenormalizationStage(IServiceProvider serviceProvider, string parsedStageDirectory) =>
        serviceProvider
            .GetRequiredService<DenormalizingPipeline>()
            .Run(new DenormalizingRunOptions(
                this.fileSystem.Path.Combine(parsedStageDirectory, WeatherCsvOutputPaths.NormalizedColumnsDirectoryName),
                parsedStageDirectory,
                this.settings.RunInParallel));

    private void RunTimeNormalizationStage(
        IServiceProvider serviceProvider,
        string parsedStageDirectory,
        string timeNormalizedStageDirectory,
        string logDateTime) =>
        serviceProvider
            .GetRequiredService<TimeNormalizingPipeline>()
            .Run(new TimeNormalizingRunOptions(
                parsedStageDirectory,
                timeNormalizedStageDirectory,
                this.StageHtmlReportPath(timeNormalizedStageDirectory, "result", logDateTime),
                this.settings.RunInParallel));

    private void RunHtmlLogCsvComparisonStage(IServiceProvider serviceProvider) =>
        serviceProvider
            .GetRequiredService<CsvComparisonOutput>()
            .CompareChain(this.fileSystem.Directory.GetCurrentDirectory());

    private StageServiceProviderFactory CreateParsedStageServices(
        string parsedStageDirectory,
        string textLogFilePath) =>
        StageServiceProviderFactory.Create(
            this.configuration,
            this.fileSystem,
            parsedStageDirectory,
            textLogFilePath,
            services =>
            {
                services.AddParserServices();
                services.AddDenormalizerServices();

                if (this.settings.RunAnalysis)
                {
                    services.AddAnalysisServices();
                }

                if (this.settings.RunHtmlLogCsvComparison)
                {
                    services.AddHtmlLogCsvComparerServices();
                }
            });

    private StageServiceProviderFactory CreateTimeNormalizedStageServices(
        string timeNormalizedStageDirectory,
        string textLogFilePath) =>
        StageServiceProviderFactory.Create(
            this.configuration,
            this.fileSystem,
            timeNormalizedStageDirectory,
            textLogFilePath,
            services =>
            {
                services.AddTimeNormalizerServices();

                if (this.settings.RunAnalysis)
                {
                    services.AddAnalysisServices();
                }
            });

    private string StageTextLogPath(string stageDirectory, string logDateTime) =>
        this.fileSystem.Path.Combine(stageDirectory, $"log{logDateTime}.log");

    private string StageHtmlReportPath(string stageDirectory, string resultPrefix, string logDateTime) =>
        this.fileSystem.Path.Combine(stageDirectory, $"{resultPrefix}{logDateTime}.html");
}
