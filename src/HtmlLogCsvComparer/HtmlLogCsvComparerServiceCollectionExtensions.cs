// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

using Common;
using Microsoft.Extensions.DependencyInjection;

public static class HtmlLogCsvComparerServiceCollectionExtensions
{
    public static IServiceCollection AddHtmlLogCsvComparerServices(this IServiceCollection services)
    {
        Argument.ThrowIfNull(services);

        services.AddSingleton<CsvTreeComparer>();
        services.AddSingleton<HtmlLogDirectoryDiscovery>();
        services.AddSingleton<CsvComparisonOutput>();
        return services;
    }
}
