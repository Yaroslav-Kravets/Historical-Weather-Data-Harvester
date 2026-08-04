// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer.Tests;

using System.IO.Abstractions;
using System.IO.Compression;
using System.Reflection;
using FileSystem.TestSupport;
using Xunit;

public sealed class CsvTreeSourceTests
{
    [Fact]
    public void CollectCsvPaths_Zip_DisposesFileStreamBeforeReturning()
    {
        var inner = InMemoryFileSystem.Create();
        var zipPath = InMemoryFileSystem.UnderRoot(inner, "HtmlLog_2026-01-02_03-04-05.zip");
        CreateZip(inner, zipPath, ("parsed/Kyiv.csv", "DateTime,Temperature\n2020-01-01,1\n"));

        var fileSystem = OpenReadTrackingFileSystem.Create(inner);
        var paths = new CsvTreeComparer(fileSystem).CollectCsvPaths(zipPath);

        Assert.Single(paths);
        Assert.NotEmpty(fileSystem.OpenedReadStreams);
        Assert.All(fileSystem.OpenedReadStreams, stream => Assert.True(stream.IsDisposed));
    }

    [Fact]
    public void CompareCsvTrees_Zip_DisposesFileStreamsAfterLoading()
    {
        var inner = InMemoryFileSystem.Create();
        var zipPath = InMemoryFileSystem.UnderRoot(inner, "HtmlLog_2026-01-02_03-04-05.zip");
        CreateZip(inner, zipPath, ("parsed/Kyiv.csv", "DateTime,Temperature\n2020-01-01,1\n"));

        var fileSystem = OpenReadTrackingFileSystem.Create(inner);
        var createOpenCount = new CsvTreeComparer(fileSystem).CollectCsvPaths(zipPath).Count;
        Assert.True(createOpenCount > 0);
        var opensAfterCollect = fileSystem.OpenedReadStreams.Count;

        var result = new CsvTreeComparer(fileSystem).CompareCsvTrees(zipPath, zipPath);

        Assert.True(result.IsEqual);
        Assert.True(fileSystem.OpenedReadStreams.Count > opensAfterCollect);
        Assert.All(fileSystem.OpenedReadStreams, stream => Assert.True(stream.IsDisposed));
    }

    [Fact]
    public void CollectCsvPaths_Directory_DoesNotOpenStreams_CompareDisposesThem()
    {
        var inner = InMemoryFileSystem.Create();
        var root = InMemoryFileSystem.UnderRoot(inner, "dir");
        inner.Directory.CreateDirectory(root);
        inner.File.WriteAllText(
            inner.Path.Combine(root, "a.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");

        var fileSystem = OpenReadTrackingFileSystem.Create(inner);
        var paths = new CsvTreeComparer(fileSystem).CollectCsvPaths(root);
        Assert.Single(paths);
        Assert.Empty(fileSystem.OpenedReadStreams);

        var result = new CsvTreeComparer(fileSystem).CompareCsvTrees(root, root);

        Assert.True(result.IsEqual);
        Assert.NotEmpty(fileSystem.OpenedReadStreams);
        Assert.All(fileSystem.OpenedReadStreams, stream => Assert.True(stream.IsDisposed));
    }

    private static void CreateZip(
        IFileSystem fileSystem,
        string path,
        params (string Path, string Content)[] entries)
    {
        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
        using var stream = fileSystem.File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var (entryPath, content) in entries)
        {
            var entry = archive.CreateEntry(entryPath);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
    }

    private sealed class OpenReadTrackingFileSystem : IFileSystem
    {
        private readonly IFileSystem inner;

        private OpenReadTrackingFileSystem(
            IFileSystem inner,
            IFile file,
            List<TrackingFileSystemStream> opened)
        {
            this.inner = inner;
            this.File = file;
            this.OpenedReadStreams = opened;
        }

        public IFile File { get; }

        public List<TrackingFileSystemStream> OpenedReadStreams { get; }

        public IDirectory Directory => this.inner.Directory;

        public IDirectoryInfoFactory DirectoryInfo => this.inner.DirectoryInfo;

        public IDriveInfoFactory DriveInfo => this.inner.DriveInfo;

        public IFileInfoFactory FileInfo => this.inner.FileInfo;

        public IFileStreamFactory FileStream => this.inner.FileStream;

        public IFileSystemWatcherFactory FileSystemWatcher => this.inner.FileSystemWatcher;

        public IFileVersionInfoFactory FileVersionInfo => this.inner.FileVersionInfo;

        public IPath Path => this.inner.Path;

        public static OpenReadTrackingFileSystem Create(IFileSystem inner)
        {
            var opened = new List<TrackingFileSystemStream>();
            var file = OpenReadTrackingFileProxy.Create(inner.File, opened);
            return new OpenReadTrackingFileSystem(inner, file, opened);
        }
    }

    private sealed class OpenReadTrackingFileProxy : DispatchProxy
    {
        private IFile inner = null!;
        private List<TrackingFileSystemStream> opened = null!;

        public static IFile Create(IFile inner, List<TrackingFileSystemStream> opened)
        {
            var proxy = Create<IFile, OpenReadTrackingFileProxy>();
            var instance = (OpenReadTrackingFileProxy)(object)proxy;
            instance.inner = inner;
            instance.opened = opened;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IFile.OpenRead)
                && args is { Length: 1 }
                && args[0] is string path)
            {
                var innerStream = this.inner.OpenRead(path);
                var tracking = new TrackingFileSystemStream(innerStream, path);
                this.opened.Add(tracking);
                return tracking;
            }

            return targetMethod.Invoke(this.inner, args);
        }
    }

    private sealed class TrackingFileSystemStream : FileSystemStream
    {
        public TrackingFileSystemStream(Stream inner, string path)
            : base(inner, path, isAsync: false)
        {
        }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            this.IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
