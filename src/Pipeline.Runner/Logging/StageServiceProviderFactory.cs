// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Runner.Logging;

using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

public sealed class StageServiceProviderFactory : IDisposable
{
    private const string DefaultOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContextShort}] {Message:lj}{NewLine}{Exception}";

    private readonly ServiceProvider serviceProvider;
    private readonly Serilog.ILogger stageLogger;

    private StageServiceProviderFactory(ServiceProvider serviceProvider, Serilog.ILogger stageLogger)
    {
        this.serviceProvider = serviceProvider;
        this.stageLogger = stageLogger;
    }

    public IServiceProvider ServiceProvider => this.serviceProvider;

    public static StageServiceProviderFactory Create(
        IConfiguration configuration,
        System.IO.Abstractions.IFileSystem fileSystem,
        string stageDirectory,
        string textLogFilePath,
        Action<IServiceCollection> configureStageServices)
    {
        Argument.ThrowIfNull(fileSystem);
        fileSystem.Directory.CreateDirectory(stageDirectory);

        var fileSection = configuration.GetSection("Serilog:File");
        var fileMinimumLevel = ParseLevel(fileSection["MinimumLevel"]);
        var outputTemplate = fileSection["OutputTemplate"] ?? DefaultOutputTemplate;

        var serilogLogger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Logger(fileLogger => fileLogger
                .Enrich.With(new ShortSourceContextEnricher())
                .WriteTo.Async(a => a.File(
                    fileSystem,
                    textLogFilePath,
                    restrictedToMinimumLevel: fileMinimumLevel,
                    outputTemplate: outputTemplate)))
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton(fileSystem);
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();

            // dispose: false — must stay paired with stageLogger dispose in Dispose() below
            // so Async buffers / file handles are flushed exactly once (not leaked or double-disposed).
            loggingBuilder.AddSerilog(serilogLogger, dispose: false);
        });
        configureStageServices(services);

        return new StageServiceProviderFactory(services.BuildServiceProvider(), serilogLogger);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.serviceProvider.Dispose();

        // Paired with AddSerilog(..., dispose: false) above.
        (this.stageLogger as IDisposable)?.Dispose();
    }

    private static LogEventLevel ParseLevel(string? level) =>
        Enum.TryParse(level, ignoreCase: true, out LogEventLevel parsed)
            ? parsed
            : LogEventLevel.Debug;
}
