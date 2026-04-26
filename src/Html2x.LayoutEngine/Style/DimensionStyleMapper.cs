using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Measurements.Dimensions;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Maps raw CSS width/height declarations into the shared dimension contracts.
/// </summary>
public sealed class DimensionStyleMapper(ICssValueConverter converter)
{
    private readonly ICssValueConverter _converter = converter ?? throw new ArgumentNullException(nameof(converter));

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

    public void ApplyDimensions(
        ICssStyleDeclaration css,
        IElement element,
        ComputedStyleBuilder style,
        DiagnosticsSession? diagnosticsSession)
    {
        ArgumentNullException.ThrowIfNull(css);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(style);

        var width = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.Width, element, diagnosticsSession);
        if (width.HasValue)
        {
            style.WidthPt = width.Value;
        }

        var minWidth = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.MinWidth, element, diagnosticsSession);
        if (minWidth.HasValue)
        {
            style.MinWidthPt = minWidth.Value;
        }

        var maxWidth = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.MaxWidth, element, diagnosticsSession);
        if (maxWidth.HasValue)
        {
            style.MaxWidthPt = maxWidth.Value;
        }

        var height = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.Height, element, diagnosticsSession);
        if (height.HasValue)
        {
            style.HeightPt = height.Value;
        }

        var minHeight = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.MinHeight, element, diagnosticsSession);
        if (minHeight.HasValue)
        {
            style.MinHeightPt = minHeight.Value;
        }

        var maxHeight = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.MaxHeight, element, diagnosticsSession);
        if (maxHeight.HasValue)
        {
            style.MaxHeightPt = maxHeight.Value;
        }
    }

    private float? GetDimensionWithLogging(
        ICssStyleDeclaration css,
        string property,
        IElement element,
        DiagnosticsSession? diagnosticsSession)
    {
        var rawValue = InlineStyleSource.GetValue(element, property) ?? css.GetPropertyValue(property);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var trimmed = rawValue.Trim();

        if (string.Equals(trimmed, HtmlCssConstants.CssValues.None, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var unsupportedUnit = CssLengthUnitClassifier.DetectUnsupportedUnit(trimmed);
        if (unsupportedUnit != null)
        {
            StyleDiagnosticEmitter.Emit(
                diagnosticsSession,
                "style/unsupported-declaration",
                element,
                property,
                trimmed,
                null,
                "Unsupported",
                $"Unsupported unit '{unsupportedUnit}' for {property}.");
            return null;
        }

        if (!_converter.TryGetLengthPt(rawValue, out var points))
        {
            StyleDiagnosticEmitter.Emit(
                diagnosticsSession,
                "style/ignored-declaration",
                element,
                property,
                trimmed,
                null,
                "Ignored",
                $"Unable to parse {property} as a supported length.");
            return null;
        }

        if (points < 0)
        {
            StyleDiagnosticEmitter.Emit(
                diagnosticsSession,
                "style/ignored-declaration",
                element,
                property,
                trimmed,
                points.ToString(CultureInfo.InvariantCulture),
                "Ignored",
                $"Negative dimension value for {property} was ignored.");
            return null;
        }

        return points;
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
