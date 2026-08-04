// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.TimeNormalizer;

using Common;
using HtmlLog;
using Microsoft.Extensions.DependencyInjection;
using Pipeline.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTimeNormalizerServices(this IServiceCollection services)
    {
        Argument.ThrowIfNull(services);

        services.AddPipelineCoreServices();
        services.AddHtmlLogServices();
        services.AddSingleton<TimeNormalizingPipeline>();
        services.AddSingleton<ParsedSourceFilesManifestReader>();
        services.AddSingleton<PlaceTimeNormalizer>();
        services.AddSingleton<ObservationTimeNormalizer>();
        services.AddSingleton<ObservationTimeInterpolator>();
        services.AddSingleton<TimeNormalizingReportWriter>();
        services.AddSingleton<TimeNormalizingPlaceErrorCountsBuilder>();

        return services;
    }
}
