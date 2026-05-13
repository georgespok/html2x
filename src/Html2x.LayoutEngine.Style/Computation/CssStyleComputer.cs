using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Style.Models;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Style.Computation;

/// <summary>
///     Computes simplified CSS styles for supported HTML tags using AngleSharp's computed style API.
/// </summary>
internal sealed class CssStyleComputer
{
    private readonly BorderStyleMapper _borderMapper;
    private readonly CssValueParser _parser;
    private readonly DimensionStyleMapper _dimensionMapper;
    private readonly SpacingStyleMapper _spacingMapper;
    private readonly StyleTraversal _traversal;

    public CssStyleComputer()
        : this(new(), new())
    {
    }

    internal CssStyleComputer(StyleTraversal traversal, CssValueParser parser)
    {
        _traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _dimensionMapper = new(parser);
        _spacingMapper = new(parser);
        _borderMapper = new(parser);
    }

    public StyleTree Compute(
        IDocument doc,
        IDiagnosticsSink? diagnosticsSink = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tree = new StyleTree();

        if (doc.Body is not IElement body)
        {
            return tree;
        }

        var bodyStyle = body.ComputeCurrentStyle();
        ApplyPageMargins(tree, bodyStyle, body, diagnosticsSink);

        tree.Root = _traversal.Build(
            body,
            (element, parentStyle) => MapStyle(element, parentStyle, diagnosticsSink),
            diagnosticsSink, cancellationToken);
        return tree;
    }

    private ComputedStyle MapStyle(
        IElement element,
        ComputedStyle? parentStyle,
        IDiagnosticsSink? diagnosticsSink)
    {
        var css = element.ComputeCurrentStyle();

        var style = new ComputedStyleBuilder();

        ApplyTypography(css, element, style, parentStyle, diagnosticsSink);
        _spacingMapper.ApplySpacing(css, element, style, diagnosticsSink);
        _dimensionMapper.ApplyDimensions(css, element, style, diagnosticsSink);
        ApplyDisplay(css, style);
        ApplyFloat(css, style);
        ApplyPosition(css, style);

        _borderMapper.ApplyBorders(css, element, style, diagnosticsSink);
        return style.Build();
    }

    private static void ApplyDisplay(ICssStyleDeclaration css, ComputedStyleBuilder style)
    {
        var display = css.GetPropertyValue(HtmlCssVocabulary.CssProperties.Display);
        style.Display = string.IsNullOrWhiteSpace(display)
            ? null
            : display.Trim().ToLowerInvariant();
    }

    private static void ApplyFloat(ICssStyleDeclaration css, ComputedStyleBuilder style)
    {
        var rawFloat = css.GetPropertyValue(HtmlCssVocabulary.CssProperties.Float);
        var normalized = string.IsNullOrWhiteSpace(rawFloat)
            ? HtmlCssVocabulary.Defaults.FloatDirection
            : rawFloat.Trim().ToLowerInvariant();
        style.FloatDirection = normalized switch
        {
            HtmlCssVocabulary.CssValues.Left => HtmlCssVocabulary.CssValues.Left,
            HtmlCssVocabulary.CssValues.Right => HtmlCssVocabulary.CssValues.Right,
            _ => HtmlCssVocabulary.Defaults.FloatDirection
        };
    }

    private static void ApplyPosition(ICssStyleDeclaration css, ComputedStyleBuilder style)
    {
        var rawPosition = css.GetPropertyValue(HtmlCssVocabulary.CssProperties.Position);
        var normalized = string.IsNullOrWhiteSpace(rawPosition)
            ? HtmlCssVocabulary.Defaults.Position
            : rawPosition.Trim().ToLowerInvariant();

        style.Position = normalized switch
        {
            HtmlCssVocabulary.CssValues.Absolute => HtmlCssVocabulary.CssValues.Absolute,
            HtmlCssVocabulary.CssValues.Relative => HtmlCssVocabulary.CssValues.Relative,
            HtmlCssVocabulary.CssValues.Static => HtmlCssVocabulary.CssValues.Static,
            _ => HtmlCssVocabulary.Defaults.Position
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
            HtmlCssVocabulary.CssProperties.Margin,
            HtmlCssVocabulary.CssProperties.MarginTop,
            HtmlCssVocabulary.CssProperties.MarginRight,
            HtmlCssVocabulary.CssProperties.MarginBottom,
            HtmlCssVocabulary.CssProperties.MarginLeft,
            element,
            diagnosticsSink);

        tree.Page.Margin = margin;
    }

    private void ApplyTypography(
        ICssStyleDeclaration css,
        IElement element,
        ComputedStyleBuilder style,
        ComputedStyle? parentStyle,
        IDiagnosticsSink? diagnosticsSink)
    {
        style.FontFamily = NormalizeFontFamily(
            _parser.GetString(css, HtmlCssVocabulary.CssProperties.FontFamily,
                parentStyle?.FontFamily ?? HtmlCssVocabulary.Defaults.FontFamily));

        var fontSize = _parser.ParseLengthPt(css.GetPropertyValue(HtmlCssVocabulary.CssProperties.FontSize));
        if (fontSize.HasValue)
        {
            style.FontSizePt = fontSize.Value;
        }
        else
        {
            style.FontSizePt = parentStyle?.FontSizePt ?? HtmlCssVocabulary.Defaults.DefaultFontSizePt;
        }

        style.IsBold = _parser.IsBold(_parser.GetString(css, HtmlCssVocabulary.CssProperties.FontWeight)) ||
                     (parentStyle?.IsBold ?? false);

        style.IsItalic = _parser.IsItalic(_parser.GetString(css, HtmlCssVocabulary.CssProperties.FontStyle)) ||
                       (parentStyle?.IsItalic ?? false);

        style.TextAlign = _parser.NormalizeAlign(
            _parser.GetString(css, HtmlCssVocabulary.CssProperties.TextAlign),
            parentStyle?.TextAlign ?? HtmlCssVocabulary.Defaults.TextAlign);

        style.LineHeightMultiplier = ResolveLineHeightMultiplier(css, parentStyle);

        style.Decorations = ResolveTextDecorations(css, parentStyle);

        style.Color = ResolveColor(
            css,
            element,
            HtmlCssVocabulary.CssProperties.Color,
            parentStyle?.Color ?? ColorRgba.Black,
            diagnosticsSink);

        var background = ResolveOptionalColor(
            css,
            element,
            HtmlCssVocabulary.CssProperties.BackgroundColor,
            diagnosticsSink);
        style.BackgroundColor = background;
    }

    private ColorRgba ResolveColor(
        ICssStyleDeclaration css,
        IElement element,
        string property,
        ColorRgba fallback,
        IDiagnosticsSink? diagnosticsSink)
    {
        var authored = InlineStyleSource.GetValue(element, property);
        if (!string.IsNullOrWhiteSpace(authored))
        {
            if (IsCssWideKeyword(authored))
            {
                return CssColorParser.Parse(_parser.GetString(css, property), fallback);
            }

            return TryParseAuthoredColor(element, property, authored, diagnosticsSink, out var color)
                ? color
                : fallback;
        }

        return CssColorParser.Parse(_parser.GetString(css, property), fallback);
    }

    private ColorRgba? ResolveOptionalColor(
        ICssStyleDeclaration css,
        IElement element,
        string property,
        IDiagnosticsSink? diagnosticsSink)
    {
        var authored = InlineStyleSource.GetValue(element, property);
        if (!string.IsNullOrWhiteSpace(authored))
        {
            if (IsCssWideKeyword(authored))
            {
                var computedColor = _parser.GetString(css, property);
                return string.IsNullOrWhiteSpace(computedColor)
                    ? null
                    : CssColorParser.Parse(computedColor, ColorRgba.Transparent);
            }

            return TryParseAuthoredColor(element, property, authored, diagnosticsSink, out var color)
                ? color
                : null;
        }

        var computed = _parser.GetString(css, property);
        return string.IsNullOrWhiteSpace(computed)
            ? null
            : CssColorParser.Parse(computed, ColorRgba.Transparent);
    }

    private static bool TryParseAuthoredColor(
        IElement element,
        string property,
        string rawValue,
        IDiagnosticsSink? diagnosticsSink,
        out ColorRgba color)
    {
        if (CssColorParser.TryParse(rawValue, out color))
        {
            return true;
        }

        StyleDiagnosticEmitter.EmitIgnoredDeclaration(
            diagnosticsSink,
            element,
            property,
            rawValue.Trim(),
            null,
            $"Unable to parse {property} as a supported color.");
        return false;
    }

    private static bool IsCssWideKeyword(string rawValue) => string.Equals(rawValue.Trim(),
        HtmlCssVocabulary.CssValues.Inherit, StringComparison.OrdinalIgnoreCase);

    private static TextDecorations ResolveTextDecorations(ICssStyleDeclaration css, ComputedStyle? parentStyle)
    {
        var raw = css.GetPropertyValue(HtmlCssVocabulary.CssProperties.TextDecoration);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return parentStyle?.Decorations ?? TextDecorations.None;
        }

        var normalized = raw.ToLowerInvariant();
        if (normalized.Contains(HtmlCssVocabulary.CssValues.None, StringComparison.OrdinalIgnoreCase))
        {
            return TextDecorations.None;
        }

        var decorations = TextDecorations.None;
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (string.Equals(token, HtmlCssVocabulary.CssValues.Underline, StringComparison.OrdinalIgnoreCase))
            {
                decorations |= TextDecorations.Underline;
            }
            else if (string.Equals(token, HtmlCssVocabulary.CssValues.LineThrough, StringComparison.OrdinalIgnoreCase))
            {
                decorations |= TextDecorations.LineThrough;
            }
            else if (string.Equals(token, HtmlCssVocabulary.CssValues.Overline, StringComparison.OrdinalIgnoreCase))
            {
                decorations |= TextDecorations.Overline;
            }
        }

        return decorations == TextDecorations.None ? parentStyle?.Decorations ?? TextDecorations.None : decorations;
    }

    private static float? ResolveLineHeightMultiplier(ICssStyleDeclaration css, ComputedStyle? parentStyle)
    {
        var raw = css.GetPropertyValue(HtmlCssVocabulary.CssProperties.LineHeight);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return parentStyle?.LineHeightMultiplier;
        }

        var trimmed = raw.Trim();

        if (string.Equals(trimmed, HtmlCssVocabulary.CssValues.Normal, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(trimmed, HtmlCssVocabulary.CssValues.Inherit, StringComparison.OrdinalIgnoreCase))
        {
            return parentStyle?.LineHeightMultiplier;
        }

        return float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var multiplier) &&
               multiplier > 0
            ? multiplier
            : parentStyle?.LineHeightMultiplier;
    }

    private static string NormalizeFontFamily(string familyList)
    {
        if (string.IsNullOrWhiteSpace(familyList))
        {
            return HtmlCssVocabulary.Defaults.FontFamily;
        }

        var segments = familyList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return HtmlCssVocabulary.Defaults.FontFamily;
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

        return HtmlCssVocabulary.Defaults.FontFamily;
    }
}
