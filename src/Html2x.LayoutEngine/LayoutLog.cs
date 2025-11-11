using Html2x.Core;
using Microsoft.Extensions.Logging;

namespace Html2x.LayoutEngine;

internal static class LayoutLog
{
    private static readonly Action<ILogger, int, int, int, Exception?> BuildStartMessage =
        LoggerMessage.Define<int, int, int>(
            LogLevel.Information,
            new EventId(2000, nameof(BuildStart)),
            "Building layout for {HtmlLength} characters targeting page {PageWidth}x{PageHeight}");

    private static readonly Action<ILogger, string, Exception?> StageCompleteMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(2001, nameof(StageComplete)),
            "Completed layout stage {StageName}");

    private static readonly Action<ILogger, int, Exception?> BuildCompleteMessage =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(2002, nameof(BuildComplete)),
            "Layout produced {BlockCount} block fragments");

    public static void BuildStart(ILogger logger, int htmlLength, PageSize pageSize)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            BuildStartMessage(logger, htmlLength, pageSize.Width, pageSize.Height, null);
        }
    }

    public static void StageComplete(ILogger logger, string stageName)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            StageCompleteMessage(logger, stageName, null);
        }
    }

    public static void BuildComplete(ILogger logger, int blockCount)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            BuildCompleteMessage(logger, blockCount, null);
        }
    }
}
