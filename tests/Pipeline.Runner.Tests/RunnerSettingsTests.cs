// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Runner.Tests;

using Microsoft.Extensions.Configuration;
using Pipeline.Runner.Settings;
using Xunit;

public sealed class RunnerSettingsTests
{
    [Fact]
    public void Load_UsesRunTimeNormalization_WhenNewKeyPresent()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RunTimeNormalization"] = "false",
        });

        var settings = RunnerSettings.Load(configuration);

        Assert.False(settings.RunTimeNormalization);
    }

    [Fact]
    public void Load_IgnoresRunNormalization_WhenNewKeyAbsent()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RunNormalization"] = "false",
        });

        var settings = RunnerSettings.Load(configuration);

        Assert.True(settings.RunTimeNormalization);
    }

    [Fact]
    public void Load_UsesRunTimeNormalization_WhenBothKeysPresent()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RunTimeNormalization"] = "false",
            ["RunNormalization"] = "true",
        });

        var settings = RunnerSettings.Load(configuration);

        Assert.False(settings.RunTimeNormalization);
    }

    [Fact]
    public void Load_DefaultsTrue_WhenNeitherKeyPresent()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        var settings = RunnerSettings.Load(configuration);

        Assert.True(settings.RunTimeNormalization);
    }

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
