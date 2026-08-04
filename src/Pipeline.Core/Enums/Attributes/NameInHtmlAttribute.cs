// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Enums.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class NameInHtmlAttribute : Attribute
{
    public NameInHtmlAttribute(string nameInHtml)
    {
        this.NameInHtml = nameInHtml;
    }

    public string NameInHtml { get; }
}
