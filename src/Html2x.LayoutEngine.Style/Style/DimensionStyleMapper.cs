using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Abstractions.Measurements.Dimensions;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Maps raw CSS width/height declarations into the shared dimension contracts.
/// </summary>
internal sealed class DimensionStyleMapper(CssValueConverter converter)
{
    private readonly CssValueConverter _converter = converter ?? throw new ArgumentNullException(nameof(converter));

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
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(css);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(style);

        var width = GetDimensionWithLogging(
            css,
            HtmlCssConstants.CssProperties.Width,
            element,
            diagnosticsSink);
        if (width.HasValue)
        {
            style.WidthPt = width.Value;
        }

        var minWidth = GetDimensionWithLogging(
            css,
            HtmlCssConstants.CssProperties.MinWidth,
            element,
            diagnosticsSink);
        if (minWidth.HasValue)
        {
            style.MinWidthPt = minWidth.Value;
        }

        var maxWidth = GetDimensionWithLogging(
            css,
            HtmlCssConstants.CssProperties.MaxWidth,
            element,
            diagnosticsSink);
        if (maxWidth.HasValue)
        {
            style.MaxWidthPt = maxWidth.Value;
        }

        var height = GetDimensionWithLogging(
            css,
            HtmlCssConstants.CssProperties.Height,
            element,
            diagnosticsSink);
        if (height.HasValue)
        {
            style.HeightPt = height.Value;
        }

        var minHeight = GetDimensionWithLogging(
            css,
            HtmlCssConstants.CssProperties.MinHeight,
            element,
            diagnosticsSink);
        if (minHeight.HasValue)
        {
            style.MinHeightPt = minHeight.Value;
        }

        var maxHeight = GetDimensionWithLogging(
            css,
            HtmlCssConstants.CssProperties.MaxHeight,
            element,
            diagnosticsSink);
        if (maxHeight.HasValue)
        {
            style.MaxHeightPt = maxHeight.Value;
        }
    }

    private float? GetDimensionWithLogging(
        ICssStyleDeclaration css,
        string property,
        IElement element,
        IDiagnosticsSink? diagnosticsSink)
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
            StyleDiagnostics.EmitUnsupportedDeclaration(
                diagnosticsSink,
                element,
                property,
                trimmed,
                $"Unsupported unit '{unsupportedUnit}' for {property}.");
            return null;
        }

        if (!_converter.TryGetLengthPt(rawValue, out var points))
        {
            StyleDiagnostics.EmitIgnoredDeclaration(
                diagnosticsSink,
                element,
                property,
                trimmed,
                null,
                $"Unable to parse {property} as a supported length.");
            return null;
        }

        if (points < 0)
        {
            StyleDiagnostics.EmitIgnoredDeclaration(
                diagnosticsSink,
                element,
                property,
                trimmed,
                points.ToString(CultureInfo.InvariantCulture),
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
