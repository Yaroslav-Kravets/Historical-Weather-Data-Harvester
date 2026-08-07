// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Analysis;

using Common;
using HtmlLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pipeline.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalysisServices(this IServiceCollection services)
    {
        Argument.ThrowIfNull(services);

        services.AddPipelineCoreServices();
        services.AddHtmlLogServices();
        services.TryAddSingleton<WeatherCharacteristicUsageAggregator>();
        services.TryAddSingleton<WeatherCharacteristicUsageCsvWriter>();
        services.TryAddSingleton<WeatherCharacteristicUsageReportWriter>();
        services.TryAddSingleton<AnalysisPipeline>();

        return services;
    }
}
