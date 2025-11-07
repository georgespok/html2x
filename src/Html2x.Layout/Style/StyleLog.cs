using System.Globalization;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;

namespace Html2x.Layout.Style;

internal static class StyleLog
{
    private static readonly Action<ILogger, string, string, string, Exception?> InvalidPaddingValueMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(3000, nameof(InvalidPaddingValue)),
            "Invalid padding value '{Value}' for property '{Property}' on element '{ElementTag}'. Using default value 0.");

    private static readonly Action<ILogger, string, string, string, Exception?> NegativePaddingValueMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(3001, nameof(NegativePaddingValue)),
            "Negative padding value '{Value}' for property '{Property}' on element '{ElementTag}'. Using default value 0.");

    private static readonly Action<ILogger, string, string, string, string, Exception?> UnsupportedPaddingUnitMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(3002, nameof(UnsupportedPaddingUnit)),
            "Unsupported padding unit '{Unit}' in value '{Value}' for property '{Property}' on element '{ElementTag}'. Only 'px' units are supported. Using default value 0.");

    public static void InvalidPaddingValue(ILogger logger, string property, string value, IElement element)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            InvalidPaddingValueMessage(logger, value, property, element.TagName, null);
        }
    }

    public static void NegativePaddingValue(ILogger logger, string property, float value, IElement element)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            NegativePaddingValueMessage(logger, value.ToString(CultureInfo.InvariantCulture), property, element.TagName, null);
        }
    }

    public static void UnsupportedPaddingUnit(ILogger logger, string property, string value, string unit, IElement element)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            UnsupportedPaddingUnitMessage(logger, unit, value, property, element.TagName, null);
        }
    }
}

