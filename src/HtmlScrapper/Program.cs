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

    internal static class Program
    {
        private static void Main(string[] args)
        {
            using (var srv = new Service1())
            {
                srv.StartService();

                Console.WriteLine("Press Enter to exit");
                Console.ReadLine();
            }
        }
    }
}
