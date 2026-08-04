// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using Common;
using HtmlLog;
using Microsoft.Extensions.DependencyInjection;
using Pipeline.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddParserServices(this IServiceCollection services)
    {
        Argument.ThrowIfNull(services);

        services.AddPipelineCoreServices();
        services.AddHtmlLogServices();
        services.AddSingleton<ParsingPipeline>();
        services.AddSingleton<RealWeatherHtmlParser>();
        services.AddSingleton<HtmlFileParser>();
        services.AddSingleton<ParseResultOrganizer>();
        services.AddSingleton<ParsedFileInfoFlattener>();
        services.AddSingleton<ParsedWeatherCharacteristicsCollector>();
        services.AddSingleton<ParsedStageManifestCsvWriter>();
        services.AddSingleton<ParsedSourceFilesManifestWriter>();
        services.AddSingleton<ParsingReportWriter>();
        services.AddSingleton<ParsingPlaceErrorCountsBuilder>();

        return services;
    }
}
