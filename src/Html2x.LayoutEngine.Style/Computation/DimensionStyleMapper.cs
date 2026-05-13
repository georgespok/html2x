using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Style.Models;

namespace Html2x.LayoutEngine.Style.Computation;

/// <summary>
///     Maps raw CSS width/height declarations into the shared dimension contracts.
/// </summary>
internal sealed class DimensionStyleMapper(CssValueParser parser)
{
    private readonly CssLengthDeclarationReader _lengthReader = new(parser);

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
            HtmlCssVocabulary.CssProperties.Width,
            element,
            diagnosticsSink);
        if (width.HasValue)
        {
            style.WidthPt = width.Value;
        }

        var minWidth = GetDimensionWithLogging(
            css,
            HtmlCssVocabulary.CssProperties.MinWidth,
            element,
            diagnosticsSink);
        if (minWidth.HasValue)
        {
            style.MinWidthPt = minWidth.Value;
        }

        var maxWidth = GetDimensionWithLogging(
            css,
            HtmlCssVocabulary.CssProperties.MaxWidth,
            element,
            diagnosticsSink);
        if (maxWidth.HasValue)
        {
            style.MaxWidthPt = maxWidth.Value;
        }

        var height = GetDimensionWithLogging(
            css,
            HtmlCssVocabulary.CssProperties.Height,
            element,
            diagnosticsSink);
        if (height.HasValue)
        {
            style.HeightPt = height.Value;
        }

        var minHeight = GetDimensionWithLogging(
            css,
            HtmlCssVocabulary.CssProperties.MinHeight,
            element,
            diagnosticsSink);
        if (minHeight.HasValue)
        {
            style.MinHeightPt = minHeight.Value;
        }

        var maxHeight = GetDimensionWithLogging(
            css,
            HtmlCssVocabulary.CssProperties.MaxHeight,
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
        var rawValue = _lengthReader.GetValue(css, element, property);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var trimmed = rawValue.Trim();

        if (string.Equals(trimmed, HtmlCssVocabulary.CssValues.None, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!_lengthReader.TryParseLengthToken(
                trimmed,
                element,
                property,
                $"Unable to parse {property} as a supported length.",
                diagnosticsSink,
                out var points))
        {
            return null;
        }

        if (points < 0)
        {
            StyleDiagnosticEmitter.EmitIgnoredDeclaration(
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
}