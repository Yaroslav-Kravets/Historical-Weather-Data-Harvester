// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv;

using System.Globalization;
using System.IO.Abstractions;
using Common;
using CsvHelper;

public sealed class CsvRecordWriter
{
    private readonly IFileSystem fileSystem;

    public CsvRecordWriter(IFileSystem fileSystem)
    {
        Argument.ThrowIfNull(fileSystem);

        this.fileSystem = fileSystem;
    }

    public int WriteRecords<T>(
        string outputDirectory,
        string fileName,
        IEnumerable<T> records,
        Action<CsvContext>? configure = null)
    {
        Argument.ThrowIfNull(outputDirectory);
        Argument.ThrowIfNull(fileName);
        Argument.ThrowIfNull(records);
        this.fileSystem.Directory.CreateDirectory(outputDirectory);

        var csvPath = this.fileSystem.Path.Combine(outputDirectory, fileName);
        var rows = records.ToList();

        using var writer = CsvFileStreams.OpenWriteStream(this.fileSystem.File, csvPath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        configure?.Invoke(csv.Context);
        csv.WriteRecords(rows);

        return rows.Count;
    }
}
