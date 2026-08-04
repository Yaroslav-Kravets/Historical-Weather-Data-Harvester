// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Enums.Attributes;

using Common;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class AlternateNameAttribute : Attribute
{
    public AlternateNameAttribute(string name)
    {
        Argument.ThrowIf(
            name,
            string.IsNullOrWhiteSpace,
            "Name must not be null or whitespace.",
            nameof(name));
        this.Name = name;
    }

    public string Name { get; }
}
