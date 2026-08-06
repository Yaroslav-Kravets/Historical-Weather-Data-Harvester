// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Runner.Settings;

using Common;
using Microsoft.Extensions.Configuration;

public sealed class RunnerSettings
{
    public string HistoricalWeatherFilesRoot { get; set; } = string.Empty;

    public bool RunInParallel { get; set; }

    public bool RunTimeNormalization { get; set; } = true;

    public bool RunHtmlLogCsvComparison { get; set; } = true;

    public bool RunAnalysis { get; set; } = true;

    public static RunnerSettings Load(IConfiguration configuration)
    {
        Argument.ThrowIfNull(configuration);

        var settings = new RunnerSettings();
        configuration.Bind(settings);
        return settings;
    }
}
