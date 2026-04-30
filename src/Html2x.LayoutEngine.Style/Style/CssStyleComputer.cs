using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Computes simplified CSS styles for supported HTML tags using AngleSharp's computed style API.
/// </summary>
public sealed class CssStyleComputer
{
    private readonly StyleTraversal _traversal;
    private readonly CssValueConverter _converter;
    private readonly DimensionStyleMapper _dimensionMapper;
    private readonly SpacingStyleMapper _spacingMapper;
    private readonly BorderStyleMapper _borderMapper;
    
    public CssStyleComputer()
        : this(new StyleTraversal(), new CssValueConverter())
    {
    }

    internal CssStyleComputer(StyleTraversal traversal, CssValueConverter converter)
    {
        _traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _dimensionMapper = new DimensionStyleMapper(converter);
        _spacingMapper = new SpacingStyleMapper(converter);
        _borderMapper = new BorderStyleMapper(converter);
    }

    public StyleTree Compute(
        IDocument doc,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        var tree = new StyleTree();

        if (doc.Body is not IElement body)
        {
            return tree;
        }

        var bodyStyle = body.ComputeCurrentStyle();
        ApplyPageMargins(tree, bodyStyle, body, diagnosticsSink);

        tree.Root = _traversal.Build(
            body,
            (element, parentStyle) => MapStyle(element, parentStyle, diagnosticsSink));
        return tree;
    }

    private ComputedStyle MapStyle(
        IElement element,
        ComputedStyle? parentStyle,
        IDiagnosticsSink? diagnosticsSink)
    {
        var css = element.ComputeCurrentStyle();

        var style = new ComputedStyleBuilder();

        ApplyTypography(css, style, parentStyle);
        _spacingMapper.ApplySpacing(css, element, style, diagnosticsSink);
        _dimensionMapper.ApplyDimensions(css, element, style, diagnosticsSink);
        ApplyDisplay(css, style);
        ApplyFloat(css, style);
        ApplyPosition(css, style);

        _borderMapper.ApplyBorders(css, style);
        return style.Build();
    }

    private static void ApplyDisplay(ICssStyleDeclaration css, ComputedStyleBuilder style)
    {
        var display = css.GetPropertyValue(HtmlCssConstants.CssProperties.Display);
        style.Display = string.IsNullOrWhiteSpace(display)
            ? null
            : display.Trim().ToLowerInvariant();
    }

    private static void ApplyFloat(ICssStyleDeclaration css, ComputedStyleBuilder style)
    {
        var rawFloat = css.GetPropertyValue(HtmlCssConstants.CssProperties.Float);
        var normalized = string.IsNullOrWhiteSpace(rawFloat)
            ? HtmlCssConstants.Defaults.FloatDirection
            : rawFloat.Trim().ToLowerInvariant();
        style.FloatDirection = normalized switch
        {
            HtmlCssConstants.CssValues.Left => HtmlCssConstants.CssValues.Left,
            HtmlCssConstants.CssValues.Right => HtmlCssConstants.CssValues.Right,
            _ => HtmlCssConstants.Defaults.FloatDirection
        };
    }

    private static void ApplyPosition(ICssStyleDeclaration css, ComputedStyleBuilder style)
    {
        var rawPosition = css.GetPropertyValue(HtmlCssConstants.CssProperties.Position);
        var normalized = string.IsNullOrWhiteSpace(rawPosition)
            ? HtmlCssConstants.Defaults.Position
            : rawPosition.Trim().ToLowerInvariant();

        style.Position = normalized switch
        {
            HtmlCssConstants.CssValues.Absolute => HtmlCssConstants.CssValues.Absolute,
            HtmlCssConstants.CssValues.Relative => HtmlCssConstants.CssValues.Relative,
            HtmlCssConstants.CssValues.Static => HtmlCssConstants.CssValues.Static,
            _ => HtmlCssConstants.Defaults.Position
        };
    }

    private void ApplyPageMargins(
        StyleTree tree,
        ICssStyleDeclaration styles,
        IElement element,
        IDiagnosticsSink? diagnosticsSink)
    {
        var margin = _spacingMapper.ParseSpacingWithOverrides(
            styles,
            HtmlCssConstants.CssProperties.Margin,
            HtmlCssConstants.CssProperties.MarginTop,
            HtmlCssConstants.CssProperties.MarginRight,
            HtmlCssConstants.CssProperties.MarginBottom,
            HtmlCssConstants.CssProperties.MarginLeft,
            element,
            diagnosticsSink);

        tree.Page.Margin = margin;
    }

    private void ApplyTypography(ICssStyleDeclaration css, ComputedStyleBuilder style, ComputedStyle? parentStyle)
    {
        style.FontFamily = NormalizeFontFamily(
            _converter.GetString(css, HtmlCssConstants.CssProperties.FontFamily,
                parentStyle?.FontFamily ?? HtmlCssConstants.Defaults.FontFamily));

        if (_converter.TryGetLengthPt(css.GetPropertyValue(HtmlCssConstants.CssProperties.FontSize), out var fs))
        {
            style.FontSizePt = fs;
        }
        else
        {
            style.FontSizePt = parentStyle?.FontSizePt ?? HtmlCssConstants.Defaults.DefaultFontSizePt;
        }

        style.Bold = _converter.IsBold(_converter.GetString(css, HtmlCssConstants.CssProperties.FontWeight)) ||
                     (parentStyle?.Bold ?? false);

        style.Italic = _converter.IsItalic(_converter.GetString(css, HtmlCssConstants.CssProperties.FontStyle)) ||
                       (parentStyle?.Italic ?? false);

        style.TextAlign = _converter.NormalizeAlign(
            _converter.GetString(css, HtmlCssConstants.CssProperties.TextAlign),
            parentStyle?.TextAlign ?? HtmlCssConstants.Defaults.TextAlign);

        style.LineHeightMultiplier = ResolveLineHeightMultiplier(css, parentStyle);

        style.Decorations = ResolveTextDecorations(css, parentStyle);

        style.Color = ColorRgba.FromCss(
            _converter.GetString(css, HtmlCssConstants.CssProperties.Color),
            parentStyle?.Color ?? ColorRgba.Black);

        var background = _converter.GetString(css, HtmlCssConstants.CssProperties.BackgroundColor);
        style.BackgroundColor = string.IsNullOrWhiteSpace(background)
            ? null
            : ColorRgba.FromCss(background, ColorRgba.Transparent);
    }

    private static TextDecorations ResolveTextDecorations(ICssStyleDeclaration css, ComputedStyle? parentStyle)
    {
        var raw = css.GetPropertyValue(HtmlCssConstants.CssProperties.TextDecoration);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return parentStyle?.Decorations ?? TextDecorations.None;
        }

        var normalized = raw.ToLowerInvariant();
        if (normalized.Contains(HtmlCssConstants.CssValues.None, StringComparison.OrdinalIgnoreCase))
        {
            return TextDecorations.None;
        }

        var decorations = TextDecorations.None;
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (string.Equals(token, "underline", StringComparison.OrdinalIgnoreCase))
            {
                decorations |= TextDecorations.Underline;
            }
            else if (string.Equals(token, "line-through", StringComparison.OrdinalIgnoreCase))
            {
                decorations |= TextDecorations.LineThrough;
            }
            else if (string.Equals(token, "overline", StringComparison.OrdinalIgnoreCase))
            {
                decorations |= TextDecorations.Overline;
            }
        }

        return decorations == TextDecorations.None ? parentStyle?.Decorations ?? TextDecorations.None : decorations;
    }

    private static float? ResolveLineHeightMultiplier(ICssStyleDeclaration css, ComputedStyle? parentStyle)
    {
        var raw = css.GetPropertyValue(HtmlCssConstants.CssProperties.LineHeight);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return parentStyle?.LineHeightMultiplier;
        }

        var trimmed = raw.Trim();

        if (string.Equals(trimmed, "normal", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(trimmed, "inherit", StringComparison.OrdinalIgnoreCase))
        {
            return parentStyle?.LineHeightMultiplier;
        }

        return float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var multiplier) && multiplier > 0
            ? multiplier
            : parentStyle?.LineHeightMultiplier;
    }

    private static string NormalizeFontFamily(string familyList)
    {
        if (string.IsNullOrWhiteSpace(familyList))
        {
            return HtmlCssConstants.Defaults.FontFamily;
        }

        var segments = familyList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return HtmlCssConstants.Defaults.FontFamily;
        }

        foreach (var segment in segments)
        {
            var trimmed = segment.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if ((trimmed.StartsWith('"') && trimmed.EndsWith('"')) ||
                (trimmed.StartsWith('\'') && trimmed.EndsWith('\'')))
            {
                trimmed = trimmed[1..^1].Trim();
            }

            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed;
            }
        }

        return HtmlCssConstants.Defaults.FontFamily;
    }

}
