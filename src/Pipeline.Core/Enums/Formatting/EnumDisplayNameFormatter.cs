// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Enums.Formatting;

using System.Collections.Concurrent;
using System.Reflection;
using Common;

public static class EnumDisplayNameFormatter
{
    private static readonly ConcurrentDictionary<(Type EnumType, ulong Value), string> DisplayNameCache = new();

    public static string ToDisplayName<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var enumType = typeof(TEnum);
        var numericValue = Convert.ToUInt64(value);

        return DisplayNameCache.GetOrAdd((enumType, numericValue), static key =>
        {
            var (type, numeric) = key;
            var enumObject = Enum.ToObject(type, numeric);
            var memberName = Enum.GetName(type, enumObject);
            if (memberName is null)
            {
                throw new ArgumentException($"Enum value '{enumObject}' is not defined on {type.Name}.");
            }

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            if (field is null)
            {
                throw new InvalidOperationException($"Enum member '{memberName}' has no reflection field on {type.Name}.");
            }

            return ToDisplayNameFromField(field, memberName);
        });
    }

    public static string ToDisplayNameFromField(FieldInfo field, string memberName)
    {
        Argument.ThrowIfNull(field);
        Argument.ThrowIfNull(memberName);
        return TryGetAttributeDisplayName(field) ?? PascalCaseNameFormatter.ToDisplayName(memberName);
    }

    private static string? TryGetAttributeDisplayName(FieldInfo? field) =>
        field?
            .GetCustomAttributes(inherit: false)
            .OfType<IEnumDisplayNameAttribute>()
            .Select(attribute => attribute.DisplayName)
            .FirstOrDefault(displayName => !string.IsNullOrWhiteSpace(displayName));
}
