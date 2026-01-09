using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Abstractions.Measurements.Dimensions;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Maps raw CSS width/height declarations into the shared dimension contracts.
/// </summary>
public sealed class DimensionStyleMapper
{
    private readonly ICssValueConverter _converter;

    public DimensionStyleMapper(ICssValueConverter converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public RequestedDimension CreateRequestedDimension(IElement element, ICssStyleDeclaration styles)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(styles);

        var (widthRaw, widthUnit) = ParseRawValue(styles.GetPropertyValue(HtmlCssConstants.CssProperties.Width));
        var (heightRaw, heightUnit) = ParseRawValue(styles.GetPropertyValue(HtmlCssConstants.CssProperties.Height));

        var chosenUnit = widthUnit != DimensionUnitEnum.Auto
            ? widthUnit
            : heightUnit;

        return new RequestedDimension(
            GetElementId(element),
            widthRaw,
            heightRaw,
            chosenUnit,
            element.GetAttribute(HtmlCssConstants.HtmlAttributes.Style));
    }

    public ResolvedDimension CreateResolvedDimension(
        RequestedDimension requested,
        float widthPt,
        float heightPt,
        bool percentageWidth,
        bool percentageHeight,
        int passCount,
        string? fallback)
    {
        ArgumentNullException.ThrowIfNull(requested);
        return new ResolvedDimension(
            requested.ElementId,
            new SizePt(widthPt, heightPt),
            percentageWidth,
            percentageHeight,
            passCount,
            fallback);
    }

    private (float?, DimensionUnitEnum) ParseRawValue(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, DimensionUnitEnum.Auto);
        }

        var trimmed = raw.Trim();

        if (trimmed.EndsWith("%", StringComparison.OrdinalIgnoreCase))
        {
            return (TryParseFloat(trimmed[..^1]), DimensionUnitEnum.Percent);
        }

        if (_converter.TryGetLengthPt(trimmed, out var points))
        {
            // CssValueConverter normalizes px to pt already.
            return (points, trimmed.EndsWith(HtmlCssConstants.CssUnits.Px, StringComparison.OrdinalIgnoreCase)
                ? DimensionUnitEnum.Px
                : DimensionUnitEnum.Pt);
        }

        return (null, DimensionUnitEnum.Auto);
    }

    private static float? TryParseFloat(string raw)
    {
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string GetElementId(IElement element)
    {
        return element.Id
               ?? element.GetAttribute("data-html2x-id")
               ?? element.TagName.ToLowerInvariant();
    }
}
