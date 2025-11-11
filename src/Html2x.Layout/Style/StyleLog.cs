using System.Globalization;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;

namespace Html2x.Layout.Style;

internal static class StyleLog
{
    private static readonly Action<ILogger, string, string, string, Exception?> InvalidSpacingValueMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(3000, nameof(InvalidSpacingValue)),
            "Invalid value '{Value}' for property '{Property}' on element '{ElementTag}'. Using default value 0.");

    private static readonly Action<ILogger, string, string, string, Exception?> NegativeSpacingValueMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(3001, nameof(NegativeSpacingValue)),
            "Negative value '{Value}' for property '{Property}' on element '{ElementTag}'. Using default value 0.");

    private static readonly Action<ILogger, string, string, string, string, Exception?> UnsupportedSpacingUnitMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(3002, nameof(UnsupportedSpacingUnit)),
            "Unsupported unit '{UnitEnum}' in value '{Value}' for property '{Property}' on element '{ElementTag}'. Only 'px' units are supported. Using default value 0.");

    public static void InvalidSpacingValue(ILogger logger, string property, string value, IElement element)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            InvalidSpacingValueMessage(logger, value, property, element.TagName, null);
        }
    }

    public static void NegativeSpacingValue(ILogger logger, string property, float value, IElement element)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            NegativeSpacingValueMessage(logger, value.ToString(CultureInfo.InvariantCulture), property, element.TagName, null);
        }
    }

    public static void UnsupportedSpacingUnit(ILogger logger, string property, string value, string unit, IElement element)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            UnsupportedSpacingUnitMessage(logger, unit, value, property, element.TagName, null);
        }
    }
}

