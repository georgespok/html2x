using Microsoft.Extensions.Logging;

namespace Html2x.Diagnostics.Logging;

public sealed class DiagnosticsLoggerOptions
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}
