// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using Common;

public sealed class ParsedFileInfoFlattener
{
    public IEnumerable<ParsedFileInfo> Flatten(
        Dictionary<string, SortedDictionary<DateTime, ParsedDateEntry>> parseResultsByPlace)
    {
        Argument.ThrowIfNull(parseResultsByPlace);
        foreach (var (place, resultsByDate) in parseResultsByPlace)
        {
            foreach (var (date, entry) in resultsByDate)
            {
                yield return new ParsedFileInfo(place, date, entry.FilePath, entry.Result);
            }
        }
    }
}
