// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser.Tests;

using System.IO.Abstractions;
using System.Text;
using FileSystem.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class RealWeatherHtmlParserTests
{
    private readonly IFileSystem fileSystem;
    private readonly RealWeatherHtmlParser parser;

    static RealWeatherHtmlParserTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public RealWeatherHtmlParserTests()
    {
        this.fileSystem = InMemoryFileSystem.Create();
        this.parser = new RealWeatherHtmlParser(
            this.fileSystem,
            NullLogger<RealWeatherHtmlParser>.Instance,
            new WeatherCharacteristicConverter());
    }

    [Fact]
    public void ParseFile_ReadsWindows1251EncodedHtml()
    {
        const string cityNameInHtml = "Киеве";
        var filePath = InMemoryFileSystem.UnderRoot(this.fileSystem, "2003-1-1.html");
        var html = BuildMinimalArchiveHtml(cityNameInHtml);
        var bytes = Encoding.GetEncoding(1251).GetBytes(html);

        this.fileSystem.Directory.CreateDirectory(this.fileSystem.Path.GetDirectoryName(filePath)!);
        using (var stream = this.fileSystem.File.Create(filePath))
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        var result = this.parser.ParseFile(filePath);

        Assert.Equal(cityNameInHtml, result.CityName);
        Assert.Equal("2003-01-01", result.Date);
        Assert.Single(result.WeatherDataRows);
    }

    private static string BuildMinimalArchiveHtml(string cityNameInHtml) =>
        $"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta http-equiv="Content-Type" content="text/html; charset=windows-1251">
        <title>Архив погоды в {cityNameInHtml}. Погода за 1 январь 2003 года</title>
        </head>
        <body>
        <table class="archive_table table">
        <tr>
        <td class="at_l at_time">00:00</td>
        <td><div class="ov_hide">ясно</div></td>
        <td>-12°C</td>
        <td><img alt="северный" /> 2</td>
        <td>750</td>
        <td>70</td>
        </tr>
        </table>
        </body>
        </html>
        """;
}
