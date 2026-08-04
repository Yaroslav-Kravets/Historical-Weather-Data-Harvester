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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using Common;

    [XmlRoot("Options")]
    public sealed class Options
    {
        /// <summary>
        /// Gets or sets the start date for downloading historical weather through the present.
        /// </summary>
        public DateTime DateFrom { get; set; } = new DateTime(2012, 01, 01);

        public Range DownloadsAtOneGo { get; set; } = new Range();

        public Range SecondsBetweenDays { get; set; } = new Range();

        public Range SecondsBetweenDownloads { get; set; } = new Range();

        public Range SecondsToFirstDownload { get; set; } = new Range();

        [XmlArray("Regions")]
        [XmlArrayItem("Region", typeof(RegionOptions))]
        public List<RegionOptions> Regions { get; set; } = new List<RegionOptions>();

        public static Options Deserialize(string sPath)
        {
            Argument.ThrowIfNull(sPath);
            using (TextReader tr = new StreamReader(sPath))
            {
                XmlSerializer xr = new XmlSerializer(typeof(Options));
                return (Options)xr.Deserialize(tr);
            }
        }
    }
}
