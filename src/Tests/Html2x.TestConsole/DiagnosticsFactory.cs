using System.IO;
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

        var resolvedJson = options.DiagnosticsJson is null
            ? null
            : Path.GetFullPath(options.DiagnosticsJson);

        if (resolvedJson is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(resolvedJson)!);
            logger.LogInformation("Diagnostics JSON output: {Path}", resolvedJson);
        }

        logger.LogInformation("Diagnostics enabled.");

        var diagnosticsOptions = new DiagnosticsOptions
        {
            JsonOutputPath = resolvedJson,
            EnableConsoleSink = true
        };

        return diagnosticsOptions.BuildRuntime();
    }
}


