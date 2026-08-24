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
using Common;
using FileSystem.TestSupport;
using HtmlLogCsvComparer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pipeline.Runner.Settings;
using Xunit;

public sealed class HtmlLogCsvComparisonStageTests
{
    private const string IdenticalCsv = "DateTime,Temperature\n2020-01-01,1\n";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RunHtmlLogCsvComparisonStage_ForwardsVerboseSettingToCompareChain(bool verbose)
    {
        var fileSystem = InMemoryFileSystem.Create();
        var cwd = InMemoryFileSystem.UnderRoot(fileSystem, "cwd");
        fileSystem.Directory.CreateDirectory(cwd);
        fileSystem.Directory.SetCurrentDirectory(cwd);

        SeedEqualHtmlLogPair(fileSystem, cwd);

        var logger = new CollectingLogger<CsvComparisonOutput>();
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IFileSystem>(fileSystem)
            .AddSingleton<ILogger<CsvComparisonOutput>>(logger)
            .AddSingleton<CsvTreeComparer>()
            .AddSingleton<HtmlLogDirectoryDiscovery>()
            .AddSingleton<CsvComparisonOutput>()
            .BuildServiceProvider();

        var runner = new PipelineRunner(
            new ConfigurationBuilder().Build(),
            fileSystem,
            new RunnerSettings { HtmlLogCsvComparisonVerbose = verbose });

        runner.RunHtmlLogCsvComparisonStage(serviceProvider);

        var equal = Assert.Single(logger.Messages, message => message.Contains("— EQUAL", StringComparison.Ordinal));
        if (verbose)
        {
            Assert.Contains("\"matched\"", equal, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("\"matched\"", equal, StringComparison.Ordinal);
        }
    }

    private static void SeedEqualHtmlLogPair(IFileSystem fileSystem, string cwd)
    {
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        foreach (var name in new[] { firstName, secondName })
        {
            var parsed = fileSystem.Path.Combine(cwd, name, "parsed");
            fileSystem.Directory.CreateDirectory(parsed);
            fileSystem.File.WriteAllText(fileSystem.Path.Combine(parsed, "Kyiv.csv"), IdenticalCsv);
        }
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            this.Messages.Add(formatter(state, exception));

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
