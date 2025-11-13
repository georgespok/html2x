using Html2x.Abstractions.Layout.Fragments;
using Microsoft.Extensions.Logging;

namespace Html2x.Renderers.Pdf.Pipeline;

internal static class PdfRendererLog
{
    private static readonly Action<ILogger, int, int, Exception?> RenderingLayout =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(1000, nameof(RenderingLayout)),
            "Rendering layout with {PageCount} pages (options hash: {OptionsHash})");

    private static readonly Action<ILogger, int, float, float, Exception?> RenderingPage =
        LoggerMessage.Define<int, float, float>(
            LogLevel.Debug,
            new EventId(1001, nameof(RenderingPage)),
            "Rendering page {PageIndex} with size {Width}x{Height}");

    private static readonly Action<ILogger, string, float, float, float, Exception?> RenderingFragment =
        LoggerMessage.Define<string, float, float, float>(
            LogLevel.Trace,
            new EventId(1002, nameof(RenderingFragment)),
            "Rendering fragment {FragmentType} at ({X},{Y}) height {Height}");

    private static readonly Action<ILogger, string, Exception?> UnsupportedFragment =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1003, nameof(UnsupportedFragment)),
            "Encountered unsupported fragment type {FragmentType}");

    private static readonly Action<ILogger, Exception?> RenderingError =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1004, nameof(RenderingError)),
            "Renderer threw unhandled exception");

    public static void LayoutStart(ILogger logger, int pageCount, int optionsHash)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            RenderingLayout(logger, pageCount, optionsHash, null);
        }
    }

    public static void PageStart(ILogger logger, int pageIndex, float width, float height)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            RenderingPage(logger, pageIndex, width, height, null);
        }
    }

    public static void FragmentStart(ILogger logger, Fragment fragment)
    {
        if (!logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        var rect = fragment.Rect;
        RenderingFragment(logger, fragment.GetType().Name, rect.X, rect.Y, rect.Height, null);
    }

    public static void FragmentUnsupported(ILogger logger, Fragment fragment)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            UnsupportedFragment(logger, fragment.GetType().Name, null);
        }
    }

    public static void Exception(ILogger logger, Exception ex)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            RenderingError(logger, ex);
        }
    }
}



