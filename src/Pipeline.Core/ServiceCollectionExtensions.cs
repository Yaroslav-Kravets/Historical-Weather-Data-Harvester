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

        services.AddSingleton<CsvRecordWriter>();
        services.AddSingleton<PlaceCsvFileNameResolver>();
        services.AddSingleton<PlaceConverter>();
        services.AddSingleton<WeatherCharacteristicConverter>();
        services.AddSingleton<WeatherCharacteristicsEnglishCsvConverter>();
        services.AddSingleton<WeatherDataCsvRecordMap>();
        services.AddSingleton<NormalizedColumnsWeatherDataCsvWriter>();
        services.AddSingleton<NormalizedColumnsWeatherDataCsvReader>();
        services.AddSingleton<DenormalizedWeatherDataCsvWriter>();
        services.AddSingleton<DenormalizedWeatherDataCsvReader>();

        return services;
    }
}
