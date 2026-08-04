// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Serilog;

using System.IO.Abstractions;
using Common;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.FileSystemAbstractions;

public static class FileSystemLoggerConfigurationExtensions
{
    private const string DefaultOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    public static LoggerConfiguration File(
        this LoggerSinkConfiguration sinkConfiguration,
        IFileSystem fileSystem,
        string path,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        string outputTemplate = DefaultOutputTemplate,
        IFormatProvider? formatProvider = null,
        bool buffered = false)
    {
        Argument.ThrowIfNull(sinkConfiguration);
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(path);
        Argument.ThrowIfNull(outputTemplate);

        var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
        var sink = new FileSystemFileSink(fileSystem, path, formatter, buffered);
        return sinkConfiguration.Sink(sink, restrictedToMinimumLevel);
    }
}
