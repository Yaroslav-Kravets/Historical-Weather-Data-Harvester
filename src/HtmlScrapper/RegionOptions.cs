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
    /// <summary>
    /// Region settings.
    /// </summary>
    public sealed class RegionOptions
    {
        /// <summary>
        /// Gets or sets the region name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the directory path containing downloaded files.
        /// </summary>
        public string Directory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL format string used to download data.
        /// </summary>
        public string URLMask { get; set; } = string.Empty;
    }
}
