// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Execution;

using System.Globalization;

public static class DurationFormatter
{
    private const string HoursMinutesSecondsMillisecondsFormat = @"hh\:mm\:ss\.fff";

    public static string FormatSeconds(double totalSeconds)
    {
        return Format(TimeSpan.FromSeconds(totalSeconds));
    }

    public static string Format(TimeSpan duration)
    {
        return duration.ToString(HoursMinutesSecondsMillisecondsFormat, CultureInfo.InvariantCulture);
    }
}
