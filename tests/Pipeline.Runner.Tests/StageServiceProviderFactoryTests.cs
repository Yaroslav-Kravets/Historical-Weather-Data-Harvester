// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Runner.Tests;

using System.IO.Abstractions;
using System.Text;
using FileSystem.TestSupport;
using HtmlLog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pipeline.Denormalizer;
using Pipeline.Parser;
using Pipeline.Runner.Logging;
using Pipeline.TimeNormalizer;
using Xunit;

public sealed class StageServiceProviderFactoryTests
{
    [Fact]
    public void Create_PropagatesInjectedFileSystemToStageServices()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var otherFileSystem = InMemoryFileSystem.Create();
        var stageDirectory = InMemoryFileSystem.UnderRoot(fileSystem, "stage");
        var logPath = fileSystem.Path.Combine(stageDirectory, "stage.log");
        var configuration = new ConfigurationBuilder().Build();

        using var stage = StageServiceProviderFactory.Create(
            configuration,
            fileSystem,
            stageDirectory,
            logPath,
            services => services.AddParserServices());

        var resolvedFileSystem = stage.ServiceProvider.GetRequiredService<IFileSystem>();
        var htmlLogFileManager = stage.ServiceProvider.GetRequiredService<HtmlLogFileManager>();
        var csvRecordWriter = stage.ServiceProvider.GetRequiredService<CsvRecordWriter>();

        Assert.Same(fileSystem, resolvedFileSystem);
        Assert.Same(fileSystem, htmlLogFileManager.FileSystem);
        Assert.NotSame(otherFileSystem, resolvedFileSystem);

        var reportPath = fileSystem.Path.Combine(stageDirectory, "result.html");
        using (var writer = new HtmlLogWriter(htmlLogFileManager, reportPath, "Stage FS Test"))
        {
            writer.WriteTable(new[] { new { Name = "ok" } }, "Rows");
        }

        Assert.True(fileSystem.File.Exists(reportPath));
        Assert.False(otherFileSystem.File.Exists(reportPath));

        var csvDirectory = fileSystem.Path.Combine(stageDirectory, "csv");
        csvRecordWriter.WriteRecords(csvDirectory, "place.csv", new[] { new { Value = 1 } });
        Assert.True(fileSystem.File.Exists(fileSystem.Path.Combine(csvDirectory, "place.csv")));
        Assert.False(otherFileSystem.File.Exists(fileSystem.Path.Combine(csvDirectory, "place.csv")));
    }

    [Fact]
    public void Create_ResolvesParsingPipelineFromRegisteredServices()
    {
        using var stage = CreateStage(services => services.AddParserServices());

        Assert.NotNull(stage.ServiceProvider.GetRequiredService<ParsingPipeline>());
    }

    [Fact]
    public void Create_ResolvesDenormalizingPipelineFromRegisteredServices()
    {
        using var stage = CreateStage(services => services.AddDenormalizerServices());

        Assert.NotNull(stage.ServiceProvider.GetRequiredService<DenormalizingPipeline>());
    }

    [Fact]
    public void Create_ResolvesTimeNormalizingPipelineFromRegisteredServices()
    {
        using var stage = CreateStage(services => services.AddTimeNormalizerServices());

        Assert.NotNull(stage.ServiceProvider.GetRequiredService<TimeNormalizingPipeline>());
    }

    [Fact]
    public void Create_RunsMinimalParsingPipelineOnInjectedFileSystem()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var fileSystem = InMemoryFileSystem.Create();
        var sourceRoot = InMemoryFileSystem.UnderRoot(fileSystem, "source", "Kyiv");
        var stageDirectory = InMemoryFileSystem.UnderRoot(fileSystem, "stage");
        var reportPath = fileSystem.Path.Combine(stageDirectory, "result.html");
        var logPath = fileSystem.Path.Combine(stageDirectory, "stage.log");

        fileSystem.Directory.CreateDirectory(sourceRoot);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(sourceRoot, "2003-1-1.html"),
            BuildMinimalArchiveHtml("Киеве", rowCount: 1),
            Encoding.UTF8);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(sourceRoot, "2003-1-2.html"),
            BuildMinimalArchiveHtml("Киеве", day: 2, monthLabel: "январь", year: 2003, rowCount: 3),
            Encoding.UTF8);

        var configuration = new ConfigurationBuilder().Build();
        using var stage = StageServiceProviderFactory.Create(
            configuration,
            fileSystem,
            stageDirectory,
            logPath,
            services => services.AddParserServices());

        var pipeline = stage.ServiceProvider.GetRequiredService<ParsingPipeline>();
        pipeline.Run(new ParsingRunOptions(sourceRoot, stageDirectory, reportPath, RunInParallel: false));

        var csvPath = fileSystem.Path.Combine(
            stageDirectory,
            WeatherCsvOutputPaths.NormalizedColumnsDirectoryName,
            "Kyiv.csv");

        Assert.True(fileSystem.File.Exists(reportPath));
        Assert.True(fileSystem.File.Exists(csvPath));
        Assert.Contains("End of summary report", fileSystem.File.ReadAllText(reportPath), StringComparison.Ordinal);
    }

    private static StageServiceProviderFactory CreateStage(Action<IServiceCollection> configureStageServices)
    {
        var fileSystem = InMemoryFileSystem.Create();
        var stageDirectory = InMemoryFileSystem.UnderRoot(fileSystem, "stage");
        var logPath = fileSystem.Path.Combine(stageDirectory, "stage.log");
        var configuration = new ConfigurationBuilder().Build();

        return StageServiceProviderFactory.Create(
            configuration,
            fileSystem,
            stageDirectory,
            logPath,
            configureStageServices);
    }

    private static string BuildMinimalArchiveHtml(
        string cityNameInHtml,
        int day = 1,
        string monthLabel = "январь",
        int year = 2003,
        int rowCount = 1)
    {
        var rows = new StringBuilder();
        for (var i = 0; i < rowCount; i++)
        {
            rows.AppendLine(
                $"""
                <tr>
                <td class="at_l at_time">{i:D2}:00</td>
                <td><div class="ov_hide">ясно</div></td>
                <td>-12°C</td>
                <td><img alt="северный" /> 2</td>
                <td>750</td>
                <td>70</td>
                </tr>
                """);
        }

        return $"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
        <title>Архив погоды в {cityNameInHtml}. Погода за {day} {monthLabel} {year} года</title>
        </head>
        <body>
        <table class="archive_table table">
        {rows}
        </table>
        </body>
        </html>
        """;
    }
}
