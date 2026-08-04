// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Common.Tests;

using System.Text.RegularExpressions;
using Xunit;

public sealed class HtmlLogRunDirectoryTests
{
    [Fact]
    public void NamePrefix_IsHtmlLog()
    {
        Assert.Equal("HtmlLog", HtmlLogRunDirectory.NamePrefix);
    }

    [Fact]
    public void ZipSearchPattern_MatchesHtmlLogZipGlob()
    {
        Assert.Equal("HtmlLog_*.zip", HtmlLogRunDirectory.ZipSearchPattern);
    }

    [Fact]
    public void FormatDirectoryName_FormatsExpectedName()
    {
        var timestamp = new DateTime(2026, 6, 10, 11, 59, 22);

        Assert.Equal(
            "HtmlLog_2026-06-10_11-59-22",
            HtmlLogRunDirectory.FormatDirectoryName(timestamp));
    }

    [Theory]
    [InlineData("HtmlLog_2026-06-10_11-59-22", true)]
    [InlineData("HtmlLog_2026-06-10_11-59-22_extra", false)]
    [InlineData("NotHtmlLog_2026-06-10_11-59-22", false)]
    public void DirectoryNameRegexPattern_MatchesOnlyValidFolderNames(string folderName, bool expectedMatch)
    {
        var regex = new Regex(HtmlLogRunDirectory.DirectoryNameRegexPattern);

        Assert.Equal(expectedMatch, regex.IsMatch(folderName));
    }
}
