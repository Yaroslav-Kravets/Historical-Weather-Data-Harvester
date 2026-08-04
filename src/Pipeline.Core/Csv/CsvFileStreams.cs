// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Csv;

using System.IO.Abstractions;
using System.Text;

public static class CsvFileStreams
{
    /// <summary>
    /// Opens a CSV for writing with UTF-8 encoding and <see cref="FileShare.Read"/>
    /// so external tools can read the file while it is being written.
    /// </summary>
    /// <returns>A <see cref="StreamWriter"/> over the CSV path with UTF-8 and <see cref="FileShare.Read"/>.</returns>
    public static StreamWriter OpenWriteStream(IFile file, string csvPath) =>
        new(
            file.Open(csvPath, FileMode.Create, FileAccess.Write, FileShare.Read),
            Encoding.UTF8);

    public static StreamReader OpenReadStream(IFile file, string csvPath) =>
        new(file.OpenRead(csvPath), Encoding.UTF8);
}
