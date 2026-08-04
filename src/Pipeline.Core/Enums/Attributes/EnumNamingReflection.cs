// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Enums.Attributes;

using System.Reflection;
using Common;

public static class EnumNamingReflection
{
    public static (string NameInHtml, TEnum Value)[] BuildNameInHtmlPairs<TEnum>()
        where TEnum : struct, Enum =>
        BuildNameInHtmlPairsCore<TEnum>(excludeNone: false);

    public static (string NameInHtml, TEnum Value)[] BuildNameInHtmlPairsExcludingNone<TEnum>()
        where TEnum : struct, Enum =>
        BuildNameInHtmlPairsCore<TEnum>(excludeNone: true);

    private static (string NameInHtml, TEnum Value)[] BuildNameInHtmlPairsCore<TEnum>(bool excludeNone)
        where TEnum : struct, Enum
    {
        var pairs = new List<(string NameInHtml, TEnum Value)>();
        var enumType = typeof(TEnum);

        foreach (var value in Enum.GetValues<TEnum>())
        {
            if (excludeNone && string.Equals(value.ToString(), "None", StringComparison.Ordinal))
            {
                continue;
            }

            var field = enumType.GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            if (field is null)
            {
                throw new InvalidOperationException($"{enumType.Name}.{value} has no reflection field.");
            }

            var attribute = field.GetCustomAttribute<NameInHtmlAttribute>();
            if (attribute is null)
            {
                throw new InvalidOperationException(
                    $"{enumType.Name}.{value} has no {nameof(NameInHtmlAttribute)}.");
            }

            Argument.ThrowIf(
                attribute.NameInHtml,
                string.IsNullOrWhiteSpace,
                $"{enumType.Name}.{value} has an empty {nameof(NameInHtmlAttribute)}.{nameof(NameInHtmlAttribute.NameInHtml)}.",
                nameof(NameInHtmlAttribute.NameInHtml));

            pairs.Add((attribute.NameInHtml, value));
        }

        return pairs.ToArray();
    }
}
