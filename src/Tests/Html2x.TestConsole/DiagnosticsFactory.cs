using Html2x.Diagnostics.Runtime;
using Microsoft.Extensions.Logging;

namespace Html2x.TestConsole;

internal static class DiagnosticsFactory
{
    public static DiagnosticsRuntime? Create(ConsoleOptions options, ILoggerFactory loggerFactory, ILogger logger)
    {
        if (!options.DiagnosticsEnabled)
        {
            return null;
        }

        logger.LogInformation("Diagnostics enabled.");

        return Options.DiagnosticsOptionsBuilder.Configure(options, loggerFactory, logger);
    }
}
