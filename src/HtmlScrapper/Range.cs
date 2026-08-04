// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlScrapper
{
    using System.Xml.Serialization;
    using HtmlScrapper.Helpers;

    /// <summary>
    /// Value range.
    /// </summary>
    public sealed class Range
    {
        [XmlAttribute]
        public int Min { get; set; }

        [XmlAttribute]
        public int Max { get; set; }

        public int RandomInRange()
        {
            return RandomHelper.Inst.Next(this.Min, this.Max + 1);
        }
    }
}
