// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.SourceFileSystem;

using Common;

/// <summary>
/// One source file. Always dispose this instance to release <see cref="Content"/> when opened.
/// When the source has <see cref="ISourceFileSystem.SupportsParallel"/> equal to
/// <see langword="false"/>, disposing also clears sequential-read state so the next
/// <see cref="ISourceFileSystem.OpenAll"/> step can proceed.
/// </summary>
public sealed class SourceFile : IDisposable
{
    private readonly Func<Stream>? openContent;
    private readonly Action? onDisposed;
    private Stream? content;
    private bool disposed;

    public SourceFile(string path, Stream content, Action? onDisposed = null)
    {
        Argument.ThrowIfNull(path);
        Argument.ThrowIfNull(content);

        this.Path = path;
        this.content = content;
        this.onDisposed = onDisposed;
    }

    public SourceFile(string path, Func<Stream> openContent, Action? onDisposed = null)
    {
        Argument.ThrowIfNull(path);
        Argument.ThrowIfNull(openContent);

        this.Path = path;
        this.openContent = openContent;
        this.onDisposed = onDisposed;
    }

    public string Path { get; }

    public Stream Content
    {
        get
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            if (this.content is not null)
            {
                return this.content;
            }

            this.content = this.openContent!();
            return this.content;
        }
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        try
        {
            this.content?.Dispose();
        }
        finally
        {
            this.onDisposed?.Invoke();
        }
    }
}
