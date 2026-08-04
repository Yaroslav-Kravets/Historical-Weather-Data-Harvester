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

public sealed class WeatherCharacteristicConverterTests
{
    private static readonly WeatherCharacteristicConverter StaticConverter = new();

    private readonly WeatherCharacteristicConverter converter = new();

    public static TheoryData<WeatherCharacteristics> DefinedWeatherCharacteristicsExceptNone { get; } =
        CreateDefinedWeatherCharacteristicsExceptNone();

    public static TheoryData<string, WeatherCharacteristics> AllNameInHtmlToFlagPairs { get; } =
        CreateAllNameInHtmlToFlagPairs();

    public static TheoryData<WeatherCharacteristics, string> AllFlagToEnglishDisplayNamePairs { get; } =
        CreateAllFlagToEnglishDisplayNamePairs();

    [Fact]
    public void DefinedWeatherCharacteristicsExceptNone_CoversEveryDefinedFlag()
    {
        TheoryDataCoverageAssertions.AssertCoversAllEnumValuesExcept(
            Enum.GetValues<WeatherCharacteristics>().Where(flag => flag != WeatherCharacteristics.None),
            WeatherCharacteristics.None,
            nameof(DefinedWeatherCharacteristicsExceptNone));
    }

    [Fact]
    public void AllNameInHtmlToFlagPairs_CoversEveryDefinedWeatherCharacteristic()
    {
        TheoryDataCoverageAssertions.AssertCoversAllEnumValuesExcept(
            this.converter.GetAllPairs().Select(pair => pair.Flag),
            WeatherCharacteristics.None,
            nameof(AllNameInHtmlToFlagPairs));
    }

    [Fact]
    public void AllNameInHtmlToFlagPairs_RowCountMatchesConverterTable()
    {
        TheoryDataCoverageAssertions.AssertRowCountMatchesEnumCountExcept(
            this.converter.GetAllPairs().Count,
            WeatherCharacteristics.None,
            nameof(AllNameInHtmlToFlagPairs));
    }

    [Fact]
    public void FromStrings_ThrowsArgumentNullException_WhenValuesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            this.converter.FromStrings(null!));
    }

    [Fact]
    public void FromStrings_ReturnsNone_WhenValuesIsEmpty()
    {
        var result = this.converter.FromStrings(Array.Empty<string>());
        Assert.Equal(WeatherCharacteristics.None, result);
    }

    [Fact]
    public void FromStrings_ReturnsNone_WhenValuesContainsOnlyWhitespace()
    {
        var result = this.converter.FromStrings(new[] { "  ", string.Empty, "\t" });
        Assert.Equal(WeatherCharacteristics.None, result);
    }

    [Theory]
    [MemberData(nameof(AllNameInHtmlToFlagPairs))]
    public void FromStrings_ReturnsCorrectFlag_ForEveryConfiguredName(
        string name,
        WeatherCharacteristics expected)
    {
        var result = this.converter.FromStrings(new[] { name });
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromStrings_IsCaseInsensitive()
    {
        var lower = this.converter.FromStrings(new[] { "ясно" });
        var upper = this.converter.FromStrings(new[] { "ЯСНО" });
        var mixed = this.converter.FromStrings(new[] { "ЯсНо" });
        Assert.Equal(WeatherCharacteristics.Clear, lower);
        Assert.Equal(WeatherCharacteristics.Clear, upper);
        Assert.Equal(WeatherCharacteristics.Clear, mixed);
    }

    [Fact]
    public void FromStrings_TrimsWhitespaceAroundNames()
    {
        var result = this.converter.FromStrings(new[] { "  ясно  ", "\tдождь\t" });
        Assert.Equal(WeatherCharacteristics.Clear | WeatherCharacteristics.Rain, result);
    }

    [Fact]
    public void FromStrings_CombinesMultipleValues_WithOr()
    {
        var result = this.converter.FromStrings(new[] { "дождь", "снег" });
        Assert.True(result.HasFlag(WeatherCharacteristics.Rain));
        Assert.True(result.HasFlag(WeatherCharacteristics.Snow));
        Assert.Equal(WeatherCharacteristics.Rain | WeatherCharacteristics.Snow, result);
    }

    [Fact]
    public void FromStrings_IgnoresWhitespaceEntries_AndParsesOthers()
    {
        var result = this.converter.FromStrings(new[] { "  ", "ясно", string.Empty, "\t", "дождь" });
        Assert.True(result.HasFlag(WeatherCharacteristics.Clear));
        Assert.True(result.HasFlag(WeatherCharacteristics.Rain));
    }

    [Fact]
    public void FromStrings_ThrowsInvalidOperationException_ForUnknownCharacteristic()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            this.converter.FromStrings(new[] { "неизвестная характеристика" }));
        Assert.Contains("Unknown weather characteristic", ex.Message);
        Assert.Contains("неизвестная характеристика", ex.Message);
    }

    [Fact]
    public void FromStrings_ThrowsInvalidOperationException_WithContext_WhenContextProvided()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            this.converter.FromStrings(new[] { "unknown" }, "/path/to/file.html"));
        Assert.Contains("Unknown weather characteristic", ex.Message);
        Assert.Contains("in /path/to/file.html", ex.Message);
    }

    [Fact]
    public void ToStrings_ReturnsEmptyList_ForNone()
    {
        var result = this.converter.ToStrings(WeatherCharacteristics.None);
        Assert.Empty(result);
    }

    [Fact]
    public void ToStrings_ReturnsSingleName_ForSingleFlag()
    {
        var result = this.converter.ToStrings(WeatherCharacteristics.Clear);
        Assert.Single(result);
        Assert.Equal("ясно", result[0]);
    }

    [Fact]
    public void ToStrings_ReturnsAllSetNames_ForCombinedFlags()
    {
        var flags = WeatherCharacteristics.Rain | WeatherCharacteristics.Snow;
        var result = this.converter.ToStrings(flags);
        Assert.Equal(2, result.Count);
        Assert.Contains("дождь", result);
        Assert.Contains("снег", result);
    }

    [Fact]
    public void FromStrings_Then_ToStrings_RoundTrips()
    {
        var names = new[] { "дождь", "снег", "туман" };
        var flags = this.converter.FromStrings(names);
        var back = this.converter.ToStrings(flags);
        Assert.Equal(names.Length, back.Count);
        foreach (var name in names)
        {
            Assert.Contains(back, s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ToStrings_Then_FromStrings_RoundTrips()
    {
        var flags = WeatherCharacteristics.Rain | WeatherCharacteristics.Fog | WeatherCharacteristics.Squall;
        var names = this.converter.ToStrings(flags);
        var back = this.converter.FromStrings(names);
        Assert.Equal(flags, back);
    }

    [Fact]
    public void GetAllKnownCharacteristics_ReturnsNonEmptyList()
    {
        var all = this.converter.GetAllKnownCharacteristics();
        Assert.NotEmpty(all);
    }

    [Fact]
    public void GetAllKnownCharacteristics_ReturnsNamesSortedCaseInsensitive()
    {
        var all = this.converter.GetAllKnownCharacteristics();
        var sorted = all.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(sorted, all);
    }

    [Fact]
    public void GetAllKnownCharacteristics_EveryNameRoundTripsViaFromStrings()
    {
        var all = this.converter.GetAllKnownCharacteristics();
        foreach (var name in all)
        {
            var flags = this.converter.FromStrings(new[] { name });
            var back = this.converter.ToStrings(flags);
            Assert.Contains(back, s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ConverterTable_ContainsEntryForEveryWeatherCharacteristicsEnumValue()
    {
        var flagsFromConverter = this.converter.GetAllPairs()
            .Select(pair => pair.Flag)
            .ToHashSet();

        var enumFlags = Enum.GetValues<WeatherCharacteristics>()
            .Where(flag => flag != WeatherCharacteristics.None)
            .ToList();

        Assert.Equal(enumFlags.Count, flagsFromConverter.Count);

        foreach (var flag in enumFlags)
        {
            Assert.True(
                flagsFromConverter.Contains(flag),
                $"WeatherCharacteristics.{flag} has no entry in WeatherCharacteristicConverter.");
        }
    }

    [Fact]
    public void ConverterTable_EveryFlagRoundTripsNameInHtmlToDisplayName()
    {
        var configuredEnglish = this.converter.GetAllPairs()
            .Select(pair => EnumDisplayNameFormatter.ToDisplayName(pair.Flag))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (WeatherCharacteristics flag in Enum.GetValues<WeatherCharacteristics>())
        {
            if (flag == WeatherCharacteristics.None)
            {
                continue;
            }

            var nameInHtml = this.converter.ToStrings(flag).Single();
            var english = EnumDisplayNameFormatter.ToDisplayName(flag);
            Assert.Equal(flag, this.converter.FromStrings(new[] { nameInHtml }));
            Assert.Contains(configuredEnglish, name => string.Equals(name, english, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ConverterTable_NameInHtml_IsUniqueForEveryFlag()
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
    public void ConverterTable_EnglishDisplayName_IsUniqueForEveryFlag()
    {
        var englishToFlag = new Dictionary<string, WeatherCharacteristics>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, flag) in this.converter.GetAllPairs())
        {
            var english = EnumDisplayNameFormatter.ToDisplayName(flag);

            if (!englishToFlag.TryAdd(english, flag))
            {
                Assert.Fail(
                    $"Duplicate English display name '{english}' for WeatherCharacteristics.{flag} and WeatherCharacteristics.{englishToFlag[english]}.");
            }
        }

        var expectedCount = Enum.GetValues<WeatherCharacteristics>()
            .Count(value => value != WeatherCharacteristics.None);

        Assert.Equal(expectedCount, englishToFlag.Count);
    }

    [Fact]
    public void ToEnglishCsvCell_ReturnsSortedEnglishLabels()
    {
        var flags = WeatherCharacteristics.Rain | WeatherCharacteristics.Clear;
        Assert.Equal("Clear, Rain", this.converter.ToEnglishCsvCell(flags));
    }

    [Fact]
    public void ToEnglishCsvCell_ReturnsEmptyString_ForNone()
    {
        Assert.Equal(string.Empty, this.converter.ToEnglishCsvCell(WeatherCharacteristics.None));
    }

    [Theory]
    [MemberData(nameof(AllFlagToEnglishDisplayNamePairs))]
    public void ToEnglishCsvCell_ReturnsExpectedEnglish_ForEverySingleFlag(
        WeatherCharacteristics flag,
        string expectedEnglish)
    {
        Assert.Equal(expectedEnglish, this.converter.ToEnglishCsvCell(flag));
    }

    [Theory]
    [MemberData(nameof(DefinedWeatherCharacteristicsExceptNone))]
    public void ToEnglishCsvCell_ReturnsNonEmpty_ForEveryDefinedEnumValue(WeatherCharacteristics characteristics)
    {
        var result = this.converter.ToEnglishCsvCell(characteristics);

        Assert.False(
            string.IsNullOrEmpty(result),
            $"ToEnglishCsvCell returned empty for WeatherCharacteristics.{characteristics}.");
    }

    [Fact]
    public void FromEnglishCsvCell_ReturnsNone_WhenCellIsNullOrWhitespace()
    {
        Assert.Equal(WeatherCharacteristics.None, this.converter.FromEnglishCsvCell(null));
        Assert.Equal(WeatherCharacteristics.None, this.converter.FromEnglishCsvCell(string.Empty));
        Assert.Equal(WeatherCharacteristics.None, this.converter.FromEnglishCsvCell("  "));
    }

    [Fact]
    public void FromEnglishCsvCell_RoundTrips_WithToEnglishCsvCell()
    {
        var flags = WeatherCharacteristics.Rain | WeatherCharacteristics.Fog | WeatherCharacteristics.Squall;
        var cell = this.converter.ToEnglishCsvCell(flags);
        var back = this.converter.FromEnglishCsvCell(cell);
        Assert.Equal(flags, back);
    }

    [Fact]
    public void FromEnglishCsvCell_ThrowsInvalidOperationException_ForUnknownLabel()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            this.converter.FromEnglishCsvCell("Not A Real Characteristic"));
        Assert.Contains("Unknown weather characteristic", ex.Message);
        Assert.Contains("Not A Real Characteristic", ex.Message);
    }

    [Fact]
    public void GetObservedPairs_ReturnsEmpty_ForNone()
    {
        Assert.Empty(this.converter.GetObservedPairs(WeatherCharacteristics.None));
    }

    [Fact]
    public void GetObservedPairs_ReturnsSortedEnglishNames_ForSetFlags()
    {
        var flags = WeatherCharacteristics.Rain | WeatherCharacteristics.Clear;
        var pairs = this.converter.GetObservedPairs(flags);

        Assert.Equal(2, pairs.Count);
        Assert.Equal("Clear", pairs[0].EnglishName);
        Assert.Equal("ясно", pairs[0].NameInHtml);
        Assert.Equal("Rain", pairs[1].EnglishName);
        Assert.Equal("дождь", pairs[1].NameInHtml);
    }

    private static TheoryData<WeatherCharacteristics> CreateDefinedWeatherCharacteristicsExceptNone()
    {
        var data = new TheoryData<WeatherCharacteristics>();

        foreach (var flag in Enum.GetValues<WeatherCharacteristics>())
        {
            if (flag != WeatherCharacteristics.None)
            {
                data.Add(flag);
            }
        }

        return data;
    }

    private static TheoryData<string, WeatherCharacteristics> CreateAllNameInHtmlToFlagPairs()
    {
        var data = new TheoryData<string, WeatherCharacteristics>();

        foreach (var (nameInHtml, flag) in StaticConverter.GetAllPairs())
        {
            data.Add(nameInHtml, flag);
        }

        return data;
    }

    private static TheoryData<WeatherCharacteristics, string> CreateAllFlagToEnglishDisplayNamePairs()
    {
        var data = new TheoryData<WeatherCharacteristics, string>();

        foreach (var (_, flag) in StaticConverter.GetAllPairs())
        {
            data.Add(flag, EnumDisplayNameFormatter.ToDisplayName(flag));
        }

        return data;
    }
}
