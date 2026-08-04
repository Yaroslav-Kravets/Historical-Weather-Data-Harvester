// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Converters;

using Xunit;

public sealed class WeatherCharacteristicConverterPreWarmTests
{
    private readonly WeatherCharacteristicConverter converter = new();

    [Fact]
    public void PreWarm_AllTranslationsResolveForEveryDefinedFlag()
    {
        foreach (WeatherCharacteristics flag in Enum.GetValues<WeatherCharacteristics>())
        {
            if (flag == WeatherCharacteristics.None)
            {
                continue;
            }

            var nameInHtml = this.converter.ToStrings(flag).Single();
            var displayName = EnumDisplayNameFormatter.ToDisplayName(flag);
            var englishCell = this.converter.ToEnglishCsvCell(flag);

            Assert.False(string.IsNullOrWhiteSpace(nameInHtml));
            Assert.False(string.IsNullOrWhiteSpace(displayName));
            Assert.Equal(displayName, englishCell);
            Assert.Equal(flag, this.converter.FromStrings(new[] { nameInHtml }));
            Assert.Equal(flag, this.converter.FromEnglishCsvCell(englishCell));
        }
    }

    [Fact]
    public void PreWarm_GetAllPairs_IsComplete()
    {
        var pairs = this.converter.GetAllPairs();
        var flagsFromPairs = pairs.Select(pair => pair.Flag).ToHashSet();
        var definedFlags = Enum.GetValues<WeatherCharacteristics>()
            .Where(flag => flag != WeatherCharacteristics.None)
            .ToList();

        Assert.Equal(definedFlags.Count, pairs.Count);
        Assert.Equal(definedFlags.Count, flagsFromPairs.Count);

        foreach (var flag in definedFlags)
        {
            Assert.Contains(flag, flagsFromPairs);
        }
    }

    [Fact]
    public void PreWarm_NameInHtmlValuesAreUnique()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (nameInHtml, flag) in this.converter.GetAllPairs())
        {
            Assert.True(
                seen.Add(nameInHtml),
                $"Duplicate NameInHtml '{nameInHtml}' for WeatherCharacteristics.{flag}.");
        }

        var expectedCount = Enum.GetValues<WeatherCharacteristics>()
            .Count(value => value != WeatherCharacteristics.None);

        Assert.Equal(expectedCount, seen.Count);
    }

    [Fact]
    public void PreWarm_DisplayNamesAreUnique()
    {
        var seen = new Dictionary<string, WeatherCharacteristics>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, flag) in this.converter.GetAllPairs())
        {
            var displayName = EnumDisplayNameFormatter.ToDisplayName(flag);

            if (!seen.TryAdd(displayName, flag))
            {
                Assert.Fail(
                    $"Duplicate English display name '{displayName}' for WeatherCharacteristics.{flag} and WeatherCharacteristics.{seen[displayName]}.");
            }
        }

        var expectedCount = Enum.GetValues<WeatherCharacteristics>()
            .Count(value => value != WeatherCharacteristics.None);

        Assert.Equal(expectedCount, seen.Count);
    }
}
