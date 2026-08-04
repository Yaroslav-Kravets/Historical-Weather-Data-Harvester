// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Common;

public static class HtmlLogRunDirectory
{
    public const string NamePrefix = "HtmlLog";

    public const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss";

    public const string SearchPattern = NamePrefix + "_*";

    public const string ZipSearchPattern = SearchPattern + ".zip";

    public const string DirectoryNameRegexPattern =
        "^" + NamePrefix + @"_(?<timestamp>\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})$";

    public static string FormatDirectoryName(DateTime timestamp) =>
        $"{NamePrefix}_{timestamp.ToString(TimestampFormat)}";
}
