// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Serilog.Sinks.FileSystemAbstractions;

using System.IO.Abstractions;
using System.Text;
using Common;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

/// <summary>
/// Write log events to a file via <see cref="IFileSystem"/>, mirroring
/// <c>Serilog.Sinks.File.FileSink</c> flush behavior.
/// </summary>
public sealed class FileSystemFileSink : ILogEventSink, IDisposable
{
    private readonly StreamWriter writer;
    private readonly Stream underlyingStream;
    private readonly ITextFormatter textFormatter;
    private readonly bool buffered;
    private readonly object syncRoot = new object();
    private bool isDisposed;

    public FileSystemFileSink(
        IFileSystem fileSystem,
        string path,
        ITextFormatter textFormatter,
        bool buffered = false)
    {
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(path);
        Argument.ThrowIfNull(textFormatter);

        this.textFormatter = textFormatter;
        this.buffered = buffered;

        var directory = fileSystem.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !fileSystem.Directory.Exists(directory))
        {
            fileSystem.Directory.CreateDirectory(directory);
        }

        // Single-process writer; share read so log viewers can open the file while we write.
        this.underlyingStream = fileSystem.File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        this.underlyingStream.Seek(0, SeekOrigin.End);
        this.writer = new StreamWriter(this.underlyingStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <inheritdoc/>
    public void Emit(LogEvent logEvent)
    {
        Argument.ThrowIfNull(logEvent);
        if (this.isDisposed)
        {
            return;
        }

        lock (this.syncRoot)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.textFormatter.Format(logEvent, this.writer);

            // Same policy as Serilog.Sinks.File.FileSink: always flush Fatal to disk;
            // otherwise flush the text writer unless buffering is enabled.
            if (logEvent.Level == LogEventLevel.Fatal)
            {
                this.FlushToDiskUnlocked();
            }
            else if (!this.buffered)
            {
                this.writer.Flush();
            }
        }
    }

    public void FlushToDisk()
    {
        lock (this.syncRoot)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.FlushToDiskUnlocked();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        lock (this.syncRoot)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.writer.Dispose();
            this.isDisposed = true;
        }
    }

    private void FlushToDiskUnlocked()
    {
        this.writer.Flush();
        if (this.underlyingStream is FileSystemStream fileSystemStream)
        {
            fileSystemStream.Flush(flushToDisk: true);
            return;
        }

        this.underlyingStream.Flush();
    }
}
