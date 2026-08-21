// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Enums.Formatting;

using System.Globalization;
using System.Text.RegularExpressions;

public static class PascalCaseNameFormatter
{
    private static readonly Regex PascalCaseSplitPattern = new(
        "(?<!^)(?=[A-Z])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> SplitPascalCase(string enumMemberName)
    {
        if (string.IsNullOrWhiteSpace(enumMemberName))
        {
            throw new ArgumentException("Enum member name is empty or null.", nameof(enumMemberName));
        }

        return PascalCaseSplitPattern
            .Split(enumMemberName.Trim())
            .Where(part => part.Length > 0)
            .ToList();
    }

    public static string ToDisplayName(string enumMemberName)
    {
        var parts = SplitPascalCase(enumMemberName);
        return string.Join(
            " ",
            parts.Select((part, index) => index == 0 ? NormalizeFirstPart(part) : part.ToLowerInvariant()));
    }

    private static string NormalizeFirstPart(string part)
    {
        if (part.Length == 0)
        {
            return part;
        }

        if (part.Length == 1)
        {
            return part.ToUpperInvariant();
        }

        return char.ToUpper(part[0], CultureInfo.InvariantCulture) +
               part.Substring(1).ToLowerInvariant();
    }
}
