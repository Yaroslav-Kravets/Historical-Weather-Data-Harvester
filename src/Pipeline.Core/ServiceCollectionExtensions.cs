// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core;

using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pipeline.Core.Converters;
using Pipeline.Core.Csv;
using Pipeline.Core.Csv.Readers;
using Pipeline.Core.Csv.Records;
using Pipeline.Core.Csv.Writers;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers shared Pipeline.Core CSV and converter services used by Parser, Denormalizer, TimeNormalizer, and Analysis.
    /// </summary>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPipelineCoreServices(this IServiceCollection services)
    {
        Argument.ThrowIfNull(services);

        services.TryAddSingleton<CsvRecordWriter>();
        services.TryAddSingleton<PlaceCsvFileNameResolver>();
        services.TryAddSingleton<PlaceConverter>();
        services.TryAddSingleton<WeatherCharacteristicConverter>();
        services.TryAddSingleton<WeatherCharacteristicsEnglishCsvConverter>();
        services.TryAddSingleton<WeatherDataCsvRecordMap>();
        services.TryAddSingleton<NormalizedColumnsWeatherDataCsvWriter>();
        services.TryAddSingleton<NormalizedColumnsWeatherDataCsvReader>();
        services.TryAddSingleton<DenormalizedWeatherDataCsvWriter>();
        services.TryAddSingleton<DenormalizedWeatherDataCsvReader>();

        return services;
    }
}
