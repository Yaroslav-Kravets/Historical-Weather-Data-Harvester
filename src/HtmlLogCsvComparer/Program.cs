// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

using System.IO.Abstractions;
using HtmlLogCsvComparer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.AddSerilog((_, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(builder.Configuration));
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddHtmlLogCsvComparerServices();

using var host = builder.Build();
var comparer = host.Services.GetRequiredService<CsvComparisonOutput>();
var fileSystem = host.Services.GetRequiredService<IFileSystem>();

if (args.Length == 0)
{
    comparer.WriteUsage();
    return 2;
}

var chainMode = false;
var compareMode = false;
var verbose = false;
var paths = new List<string>();

foreach (var arg in args)
{
    switch (arg)
    {
        case "--chain":
            if (chainMode || compareMode)
            {
                comparer.WriteUsage();
                return 2;
            }

            chainMode = true;
            break;
        case "--compare":
            if (chainMode || compareMode)
            {
                comparer.WriteUsage();
                return 2;
            }

            compareMode = true;
            break;
        case "--verbose":
            verbose = true;
            break;
        default:
            paths.Add(fileSystem.Path.GetFullPath(arg));
            break;
    }
}

if (chainMode && compareMode)
{
    comparer.WriteUsage();
    return 2;
}

if (chainMode)
{
    if (paths.Count != 1)
    {
        comparer.WriteUsage();
        return 2;
    }

    return comparer.CompareChain(paths[0], verbose);
}

if (compareMode || paths.Count == 2)
{
    if (paths.Count != 2)
    {
        comparer.WriteUsage();
        return 2;
    }

    return comparer.CompareDirectories(paths[0], paths[1], verbose);
}

comparer.WriteUsage();
return 2;
