// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Enums.Formatting;

using Xunit;

public sealed class PascalCaseNameFormatterTests
{
    [Theory]
    [InlineData("Clear", "Clear")]
    [InlineData("BlackIce", "Black ice")]
    [InlineData("RainAndHail", "Rain and hail")]
    [InlineData("ReducedVisibilityDueToSmoke", "Reduced visibility due to smoke")]
    [InlineData("FreezingRain", "Freezing rain")]
    [InlineData("LightFog", "Light fog")]
    public void ToDisplayName_FormatsPascalCaseEnumMember(string enumMemberName, string expected)
    {
        Assert.Equal(expected, PascalCaseNameFormatter.ToDisplayName(enumMemberName));
    }

    [Fact]
    public void SplitPascalCase_SplitsCompoundName()
    {
        var parts = PascalCaseNameFormatter.SplitPascalCase("RainAndHail");
        Assert.Equal(new[] { "Rain", "And", "Hail" }, parts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToDisplayName_Throws_ForNullOrWhitespace(string? enumMemberName)
    {
        Assert.Throws<ArgumentException>(() => PascalCaseNameFormatter.ToDisplayName(enumMemberName!));
    }
}
