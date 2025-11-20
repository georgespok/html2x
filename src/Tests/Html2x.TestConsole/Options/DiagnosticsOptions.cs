using Html2x.Diagnostics.Runtime;
using Html2x.Diagnostics.Sinks;
using Microsoft.Extensions.Logging;

namespace Html2x.TestConsole.Options;

internal static class DiagnosticsOptionsBuilder
{
    public static DiagnosticsRuntime Configure(ConsoleOptions options, ILoggerFactory loggerFactory, ILogger logger)
    {
        var runtimeBuilder = DiagnosticsRuntime.Configure(builder =>
        {
            if (!string.IsNullOrWhiteSpace(options.DiagnosticsJson))
            {
                var path = Path.GetFullPath(options.DiagnosticsJson);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                logger.LogInformation("Diagnostics JSON output: {Path}", path);
                builder.AddJsonSink(path);
            }

            builder.AddConsoleSink();
        });

        return runtimeBuilder;
    }
}
