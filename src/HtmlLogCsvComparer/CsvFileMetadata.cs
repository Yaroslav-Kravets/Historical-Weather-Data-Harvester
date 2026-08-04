// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

/// <summary>
/// Lightweight CSV probe: header, row count, and content hash without retaining row field data.
/// </summary>
internal sealed class CsvFileMetadata
{
    public CsvFileMetadata(
        string relativePath,
        string displayPath,
        long length,
        CsvHeader header,
        int rowCount,
        string contentHash)
    {
        this.RelativePath = relativePath;
        this.DisplayPath = displayPath;
        this.Length = length;
        this.Header = header;
        this.RowCount = rowCount;
        this.ContentHash = contentHash;
    }

    public string RelativePath { get; }

    public string DisplayPath { get; }

    public long Length { get; }

    public CsvHeader Header { get; }

    public int RowCount { get; }

    public string ContentHash { get; }

    public bool HasDataRows => this.RowCount > 0;

    public CsvFileSignature CreateSignature(string fileName) =>
        new(fileName, this.Header.Columns, this.RowCount);
}
