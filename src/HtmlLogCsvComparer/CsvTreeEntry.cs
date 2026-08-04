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
/// Indexed CSV entry within a directory or ZIP tree.
/// </summary>
/// <param name="OpenKey">
/// Host absolute path (directory) or ZIP entry full name (archive).
/// </param>
/// <param name="DisplayPath">Human-readable path for diagnostics.</param>
/// <param name="Length">Uncompressed byte length of the CSV.</param>
internal sealed record CsvTreeEntry(string OpenKey, string DisplayPath, long Length);
