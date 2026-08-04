// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Denormalizer;

using Common;
using Microsoft.Extensions.DependencyInjection;
using Pipeline.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDenormalizerServices(this IServiceCollection services)
    {
        Argument.ThrowIfNull(services);

        services.AddPipelineCoreServices();
        services.AddSingleton<DenormalizingPipeline>();

        return services;
    }
}
