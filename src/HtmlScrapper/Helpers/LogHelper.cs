// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlScrapper.Helpers
{
    using System;
    using System.IO;
    using System.Text;
    using Common;

    public static class LogHelper
    {
        public static void Log(MessageLevel level, string message)
        {
            Argument.ThrowIfNull(message);
            try
            {
                // http://mono-project.com/FAQ:_Technical#How_to_detect_the_execution_platform_.3F
                string sPath = string.Empty;
                int p = (int)Environment.OSVersion.Platform;
                if ((p == 4) || (p == 6) || (p == 128))
                {
                    sPath = "/media/Work/Weather/bin/RWS/log.log"; // Mono
                }
                else
                {
                    sPath = "D:\\Weather\\bin\\RWS\\log.log"; // Windows
                }

                using (StreamWriter sw = new StreamWriter(sPath, true, Encoding.UTF8))
                {
                    string s = string.Format("{0:yyyy-MM-dd HH:mm:ss}, {1}: {2}", DateTime.Now, level, message);
                    sw.WriteLine(s);
                    Console.WriteLine(s); // temp
                }
            }
            catch
            {
            }
        }
    }
}
