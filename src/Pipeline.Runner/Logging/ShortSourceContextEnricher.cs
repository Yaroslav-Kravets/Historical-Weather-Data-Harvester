// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Runner.Logging;

using Serilog.Core;
using Serilog.Events;

public sealed class ShortSourceContextEnricher : ILogEventEnricher
{
    /// <inheritdoc/>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue))
        {
            return;
        }

        var sourceContext = sourceContextValue.ToString().Trim('"');
        if (string.IsNullOrWhiteSpace(sourceContext))
        {
            return;
        }

        var shortName = sourceContext.Split('.').Last();
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SourceContextShort", shortName));
    }
}
