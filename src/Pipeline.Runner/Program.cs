// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pipeline.Runner;
using Pipeline.Runner.Settings;

var contentRoot = ContentRootResolver.Resolve(new FileSystem());
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = contentRoot,
});

builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton(_ => RunnerSettings.Load(builder.Configuration));
builder.Services.AddSingleton<PipelineRunner>();

using var host = builder.Build();
host.Services.GetRequiredService<PipelineRunner>().Run();
