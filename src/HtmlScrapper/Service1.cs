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
    using System.IO;
    using System.Linq;
    using System.Timers;
    using HtmlScrapper.Helpers;

    public sealed class Service1 : IDisposable
    {
        private Options options = null;
        private Region[] regions = null;
        private int nPeriod = 0;

        private Timer tmr = null;

        public Service1()
        {
        }

        public void StartService()
        {
            LogHelper.Log(MessageLevel.Information, "HtmlScrapper started");

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Options.config");
            this.options = Options.Deserialize(configPath);

            this.nPeriod = (DateTime.Now - this.options.DateFrom).Days;

            this.regions = new Region[this.options.Regions.Count];

            for (int i = 0; i < this.regions.Length; i++)
            {
                this.regions[i] = new Region(this.options.Regions[i], this.nPeriod, this.options);
            }

            this.tmr = new Timer();
            this.SetTimerInterval(this.options.SecondsToFirstDownload.RandomInRange());
            this.tmr.Elapsed += new ElapsedEventHandler(this.Tmr_Elapsed);
            this.tmr.Start();
        }

        public void StopService()
        {
            if (this.tmr != null)
            {
                this.tmr.Stop();
                this.tmr.Dispose();
                this.tmr = null;
            }

            LogHelper.Log(MessageLevel.Information, "HtmlScrapper stopped");
        }

        public void Dispose()
        {
            this.StopService();
        }

        private void Tmr_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.tmr.Stop();

            try
            {
                int cnt = this.regions.Where(o => o.HasToDownload).Count();

                if (cnt == 0)
                {
                    throw new Exception("No regions have days left to download");
                }

                int rnd = RandomHelper.Inst.Next(cnt) + 1;

                for (int i = 0; i < this.regions.Length; i++)
                {
                    if (this.regions[i].HasToDownload)
                    {
                        rnd--;

                        if (rnd == 0)
                        {
                            this.regions[i].Download(this.options.DownloadsAtOneGo.RandomInRange());
                            break;
                        }
                    }
                }

                if (rnd != 0)
                {
                    throw new Exception("Failed to select a region for download");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(MessageLevel.Error, ex.Message);
            }

            this.SetTimerInterval(this.options.SecondsBetweenDownloads.RandomInRange());
            this.tmr.Start();
        }

        private void SetTimerInterval(int sec)
        {
            this.tmr.Interval = sec * 1000;
            LogHelper.Log(MessageLevel.Debug, "Timer interval " + TimeSpan.FromSeconds(sec).ToString());
        }
    }
}
