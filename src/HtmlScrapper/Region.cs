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
    using System.Collections;
    using System.IO;
    using System.Net.Http;
    using HtmlScrapper.Helpers;

    internal sealed class Region
    {
        private static readonly HttpClient Http = new HttpClient();
        private RegionOptions regOpt = null;
        private Options options = null;
        private int nPeriod = 0;
        private int nToDownload = 0;
        private BitArray days = null;

        public Region(RegionOptions regOpt, int period, Options options)
        {
            this.regOpt = regOpt;
            this.options = options;
            this.nPeriod = period;

            this.days = new BitArray(this.nPeriod);

            for (int i = 0; i < this.nPeriod; i++)
            {
                this.days[i] = false;
            }

            // Mark days already present on disk
            // Count how many days still need downloading
            this.nToDownload = this.nPeriod;
            DirectoryInfo di = new DirectoryInfo(this.regOpt.Directory);
            foreach (FileInfo fi in di.GetFiles("*.html"))
            {
                string name = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);

                string[] s = name.Split('-');
                if (s.Length != 3)
                {
                    throw new Exception("Invalid file name format; expected YYYY-M-D");
                }

                DateTime dt = new DateTime(Convert.ToInt32(s[0]), Convert.ToInt32(s[1]), Convert.ToInt32(s[2]));

                int n = (dt - this.options.DateFrom).Days;
                if (n < 0 || n >= this.nPeriod)
                {
                    throw new IndexOutOfRangeException();
                }

                this.days[n] = true;
                this.nToDownload--;
            }
        }

        public bool HasToDownload
        {
            get { return this.nToDownload != 0; }
        }

        public void Download(int n)
        {
            LogHelper.Log(MessageLevel.Debug, "Attempt to download " + n + " days");

            if (this.nToDownload == 0)
            {
                throw new Exception("Nothing to download for region " + this.regOpt.Name);
            }

            // Find the r-th free day
            int r = RandomHelper.Inst.Next(this.nToDownload);
            int from = -1;
            for (int i = 0; i < this.nPeriod; i++)
            {
                if (!this.days[i])
                {
                    if (r == 0)
                    {
                        from = i;
                        break;
                    }
                    else
                    {
                        r--;
                    }
                }
            }

            if (from < 0)
            {
                return; // ??
            }

            int downloaded = 0;
            for (int i = from; i < this.nPeriod && this.nToDownload != 0 && downloaded != n; i++)
            {
                if (!this.days[i])
                {
                    this.DownloadForDay(i);
                    downloaded++;

                    if (downloaded != n)
                    {
                        int sec = this.options.SecondsBetweenDays.RandomInRange();
                        LogHelper.Log(MessageLevel.Debug, "Sleep for " + TimeSpan.FromSeconds(sec).ToString());
                        System.Threading.Thread.Sleep(sec * 1000);
                    }
                }
            }
        }

        private void DownloadForDay(int nDay)
        {
            string sDate = this.options.DateFrom.AddDays(nDay).ToString("yyyy-M-d");
            string sURL = string.Format(this.regOpt.URLMask, sDate);
            string sPath = Path.Combine(this.regOpt.Directory, sDate + ".html");

            if (File.Exists(sPath))
            {
                throw new Exception("File " + sPath + " already exists");
            }

            byte[] bytes = Http.GetByteArrayAsync(sURL).GetAwaiter().GetResult();
            File.WriteAllBytes(sPath, bytes);

            LogHelper.Log(MessageLevel.Information, "Downloaded " + sURL + " to " + sPath);

            this.days[nDay] = true;
            this.nToDownload--;
        }
    }
}
