// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLog;

using System.Collections;
using System.IO.Abstractions;
using Common;

/// <summary>
/// Owns HTML log file handlers for <see cref="HtmlLogWriter"/>.
/// One live writer per path per manager — paths must be unique while open.
/// Path keys are GetFullPath results (relative paths resolve against
/// the file system's current directory).
/// </summary>
public sealed class HtmlLogFileManager : IDisposable
{
    private readonly Dictionary<string, HtmlLogFileHandler> handlers;
    private readonly HashSet<string> pendingPaths;
    private bool isDisposed;

    public HtmlLogFileManager(IFileSystem fileSystem)
    {
        Argument.ThrowIfNull(fileSystem);

        this.FileSystem = fileSystem;
        var pathComparer = CreatePathComparer();
        this.handlers = new Dictionary<string, HtmlLogFileHandler>(pathComparer);
        this.pendingPaths = new HashSet<string>(pathComparer);
    }

    public IFileSystem FileSystem { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        List<HtmlLogFileHandler> handlersToDispose;
        lock (((ICollection)this.handlers).SyncRoot)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.pendingPaths.Clear();
            handlersToDispose = this.handlers.Values.ToList();
            this.handlers.Clear();
        }

        foreach (var handler in handlersToDispose)
        {
            handler.Dispose();
        }
    }

    /// <summary>
    /// Creates a file handler for the specified path.
    /// Throws if a handler for <paramref name="filePath"/> is already open or being created.
    /// </summary>
    /// <returns>A new <see cref="HtmlLogFileHandler"/> for the path.</returns>
    internal HtmlLogFileHandler CreateHandler(string filePath, string title)
    {
        Argument.ThrowIfNull(filePath);
        Argument.ThrowIfNull(title);

        var pathKey = this.NormalizePathKey(filePath);

        lock (((ICollection)this.handlers).SyncRoot)
        {
            ObjectDisposedException.ThrowIf(this.isDisposed, this);

            if (this.handlers.ContainsKey(pathKey) || !this.pendingPaths.Add(pathKey))
            {
                throw new InvalidOperationException(
                    $"An HTML log writer is already open for '{filePath}'. Only one live writer per path is allowed.");
            }
        }

        // Construct outside the dictionary lock so file I/O does not block other paths.
        // The pending reservation above serializes same-path creates without holding the lock during I/O.
        HtmlLogFileHandler? created = null;
        var published = false;
        try
        {
            created = new HtmlLogFileHandler(this.FileSystem, filePath, title);
            lock (((ICollection)this.handlers).SyncRoot)
            {
                this.pendingPaths.Remove(pathKey);
                ObjectDisposedException.ThrowIf(this.isDisposed, this);

                this.handlers[pathKey] = created;
                published = true;
                return created;
            }
        }
        catch
        {
            if (!published)
            {
                lock (((ICollection)this.handlers).SyncRoot)
                {
                    this.pendingPaths.Remove(pathKey);
                }

                created?.Dispose();
            }

            throw;
        }
    }

    /// <summary>
    /// Removes and disposes a file handler when no longer needed.
    /// </summary>
    internal void RemoveHandler(string filePath)
    {
        Argument.ThrowIfNull(filePath);

        var pathKey = this.NormalizePathKey(filePath);

        HtmlLogFileHandler? handler;
        lock (((ICollection)this.handlers).SyncRoot)
        {
            if (!this.handlers.Remove(pathKey, out handler))
            {
                return;
            }
        }

        handler.Dispose();
    }

    private static StringComparer CreatePathComparer()
    {
        // Windows and macOS default volumes are case-insensitive; Linux is case-sensitive.
        return OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }

    private string NormalizePathKey(string filePath)
    {
        return this.FileSystem.Path.GetFullPath(filePath);
    }
}
