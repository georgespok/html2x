using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Computes simplified CSS styles for supported HTML tags using AngleSharp's computed style API.
/// </summary>
public sealed class CssStyleComputer(
    IStyleTraversal traversal,
    IUserAgentDefaults uaDefaults,
    ICssValueConverter converter)
    : IStyleComputer
{
    private readonly IStyleTraversal _traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
    private readonly IUserAgentDefaults _uaDefaults = uaDefaults ?? throw new ArgumentNullException(nameof(uaDefaults));
    private readonly ICssValueConverter _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    
    public CssStyleComputer()
        : this(new StyleTraversal(), new UserAgentDefaults(), new CssValueConverter())
    {
    }

    public CssStyleComputer(IStyleTraversal traversal, IUserAgentDefaults uaDefaults)
        : this(traversal, uaDefaults, new CssValueConverter())
    {
    }

    public StyleTree Compute(IDocument doc)
    {
        var tree = new StyleTree();

        if (doc.Body is not IElement body)
        {
            return tree;
        }

        var bodyStyle = body.ComputeCurrentStyle();
        ApplyPageMargins(tree, bodyStyle, body);

        tree.Root = _traversal.Build(body, MapStyle);
        return tree;
    }

    private ComputedStyle MapStyle(IElement element, ComputedStyle? parentStyle)
    {
        var css = element.ComputeCurrentStyle();

        var style = new ComputedStyleBuilder();

        ApplyTypography(css, style, parentStyle);
        ApplySpacing(css, element, style);
        ApplyDimensions(css, element, style);

        _uaDefaults.Apply(element, style, parentStyle);
        ApplyBorders(css, style);
        return style.Build();
    }

    private void ApplyDimensions(ICssStyleDeclaration css, IElement element, ComputedStyleBuilder style)
    {
        var width = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.Width, element);
        if (width.HasValue)
        {
            style.WidthPt = width.Value;
        }

        var minWidth = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.MinWidth, element);
        if (minWidth.HasValue)
        {
            style.MinWidthPt = minWidth.Value;
        }

        var maxWidth = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.MaxWidth, element);
        if (maxWidth.HasValue)
        {
            style.MaxWidthPt = maxWidth.Value;
        }

        var height = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.Height, element);
        if (height.HasValue)
        {
            style.HeightPt = height.Value;
        }

        var minHeight = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.MinHeight, element);
        if (minHeight.HasValue)
        {
            style.MinHeightPt = minHeight.Value;
        }

        var maxHeight = GetDimensionWithLogging(css, HtmlCssConstants.CssProperties.MaxHeight, element);
        if (maxHeight.HasValue)
        {
            style.MaxHeightPt = maxHeight.Value;
        }
    }

    private float? GetDimensionWithLogging(ICssStyleDeclaration css, string property, IElement element)
    {
        var rawValue = css.GetPropertyValue(property);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var trimmed = rawValue.Trim();

        // Special case: "none" is valid for max-width
        if (string.Equals(trimmed, HtmlCssConstants.CssValues.None, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var unsupportedUnit = DetectUnsupportedUnit(trimmed);
        if (unsupportedUnit != null)
        {
            return null;
        }

        if (!_converter.TryGetLengthPt(rawValue, out var points))
        {
            return null;
        }

        if (points < 0)
        {
            return null;
        }

        return points;
    }

    private void ApplyPageMargins(StyleTree tree, ICssStyleDeclaration styles, IElement element)
    {
        var margin = ParseSpacingWithOverrides(
            styles,
            HtmlCssConstants.CssProperties.Margin,
            HtmlCssConstants.CssProperties.MarginTop,
            HtmlCssConstants.CssProperties.MarginRight,
            HtmlCssConstants.CssProperties.MarginBottom,
            HtmlCssConstants.CssProperties.MarginLeft,
            element);

        tree.Page.Margin = margin;
    }

    private void ApplySpacingWithOverrides(
        ICssStyleDeclaration css,
        string shorthandProperty,
        string topProperty,
        string rightProperty,
        string bottomProperty,
        string leftProperty,
        IElement element,
        Action<float> setTop,
        Action<float> setRight,
        Action<float> setBottom,
        Action<float> setLeft)
    {
        ApplySpacingShorthand(css, shorthandProperty, element, setTop, setRight, setBottom, setLeft);
        OverrideSpacingSide(css, topProperty, element, setTop);
        OverrideSpacingSide(css, rightProperty, element, setRight);
        OverrideSpacingSide(css, bottomProperty, element, setBottom);
        OverrideSpacingSide(css, leftProperty, element, setLeft);
    }

    private void OverrideSpacingSide(
        ICssStyleDeclaration css,
        string property,
        IElement element,
        Action<float> setter)
    {
        var raw = css.GetPropertyValue(property);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        setter(GetSpacingWithLogging(css, property, element));
    }

    private void ApplySpacingShorthand(
        ICssStyleDeclaration css,
        string shorthandProperty,
        IElement element,
        Action<float> setTop,
        Action<float> setRight,
        Action<float> setBottom,
        Action<float> setLeft)
    {
        var shorthandValue = css.GetPropertyValue(shorthandProperty);
        if (string.IsNullOrWhiteSpace(shorthandValue))
        {
            return;
        }

        if (!TryParseSpacingValues(shorthandProperty, shorthandValue, element, out var parsedValues))
        {
            return;
        }

        switch (parsedValues.Count)
        {
            case 1:
                setTop(parsedValues[0]);
                setRight(parsedValues[0]);
                setBottom(parsedValues[0]);
                setLeft(parsedValues[0]);
                break;
            case 2:
                setTop(parsedValues[0]);
                setBottom(parsedValues[0]);
                setRight(parsedValues[1]);
                setLeft(parsedValues[1]);
                break;
            case 3:
                setTop(parsedValues[0]);
                setRight(parsedValues[1]);
                setLeft(parsedValues[1]);
                setBottom(parsedValues[2]);
                break;
            case 4:
                setTop(parsedValues[0]);
                setRight(parsedValues[1]);
                setBottom(parsedValues[2]);
                setLeft(parsedValues[3]);
                break;
            default:
                break;
        }
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

    private void ApplySpacing(ICssStyleDeclaration css, IElement element, ComputedStyleBuilder style)
    {
        var margin = ParseSpacingWithOverrides(
            css,
            HtmlCssConstants.CssProperties.Margin,
            HtmlCssConstants.CssProperties.MarginTop,
            HtmlCssConstants.CssProperties.MarginRight,
            HtmlCssConstants.CssProperties.MarginBottom,
            HtmlCssConstants.CssProperties.MarginLeft,
            element);

        style.Margin = margin;

        var padding = ParseSpacingWithOverrides(
            css,
            HtmlCssConstants.CssProperties.Padding,
            HtmlCssConstants.CssProperties.PaddingTop,
            HtmlCssConstants.CssProperties.PaddingRight,
            HtmlCssConstants.CssProperties.PaddingBottom,
            HtmlCssConstants.CssProperties.PaddingLeft,
            element);

        style.Padding = new Spacing(
            Math.Max(0, padding.Top),
            Math.Max(0, padding.Right),
            Math.Max(0, padding.Bottom),
            Math.Max(0, padding.Left));
    }

    private Spacing ParseSpacingWithOverrides(
        ICssStyleDeclaration css,
        string shorthandProperty,
        string topProperty,
        string rightProperty,
        string bottomProperty,
        string leftProperty,
        IElement element)
    {
        var top = 0f;
        var right = 0f;
        var bottom = 0f;
        var left = 0f;

        ApplySpacingWithOverrides(
            css,
            shorthandProperty,
            topProperty,
            rightProperty,
            bottomProperty,
            leftProperty,
            element,
            value => top = value,
            value => right = value,
            value => bottom = value,
            value => left = value);

        return new Spacing(top, right, bottom, left);
    }

    private bool TryParseSpacingValues(
        string property,
        string shorthandValue,
        IElement element,
        out List<float> parsedValues)
    {
        parsedValues = [];

        var tokens = shorthandValue.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();

        if (tokens.Length == 0)
        {
            return false;
        }

        foreach (var token in tokens)
        {
            var unsupportedUnit = DetectUnsupportedUnit(token);
            if (unsupportedUnit != null)
            {
                return false;
            }

            if (!_converter.TryGetLengthPt(token, out var points))
            {
                return false;
            }

            parsedValues.Add(points);
        }

        return true;
    }

    private void ApplyBorders(ICssStyleDeclaration css, ComputedStyleBuilder style)
    {
        ApplyBorderSide(css, "top", 
            w => style.Borders.TopWidth = w, 
            s => style.Borders.TopStyle = s, 
            c => style.Borders.TopColor = c);
        
        ApplyBorderSide(css, "right", 
            w => style.Borders.RightWidth = w, 
            s => style.Borders.RightStyle = s, 
            c => style.Borders.RightColor = c);
        
        ApplyBorderSide(css, "bottom", 
            w => style.Borders.BottomWidth = w, 
            s => style.Borders.BottomStyle = s, 
            c => style.Borders.BottomColor = c);
        
        ApplyBorderSide(css, "left", 
            w => style.Borders.LeftWidth = w, 
            s => style.Borders.LeftStyle = s, 
            c => style.Borders.LeftColor = c);
    }

    private void ApplyBorderSide(
        ICssStyleDeclaration css, 
        string side, 
        Action<float> setWidth, 
        Action<BorderLineStyle> setStyle, 
        Action<ColorRgba?> setColor)
    {
        var widthRaw = css.GetPropertyValue($"border-{side}-width");
        if (_converter.TryGetLengthPt(widthRaw, out var widthPt))
        {
            setWidth(widthPt);
        }

        var styleRaw = css.GetPropertyValue($"border-{side}-style");
        setStyle(ParseBorderStyle(styleRaw));

        var colorRaw = css.GetPropertyValue($"border-{side}-color");
        setColor(ParseBorderColor(colorRaw));
    }

    private float GetSpacingWithLogging(ICssStyleDeclaration css, string property, IElement element)
    {
        var rawValue = css.GetPropertyValue(property);
        
        // If no value provided, return default (0) without logging
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return 0;
        }

        var trimmed = rawValue.Trim();

        // Check for unsupported units (non-px, non-pt)
        var unsupportedUnit = DetectUnsupportedUnit(trimmed);
        if (unsupportedUnit != null)
        {
            return 0;
        }

        // Try to parse the value
        if (!_converter.TryGetLengthPt(rawValue, out var points))
        {
            // Value provided but couldn't be parsed (non-numeric, etc.)
            return 0;
        }

        return points;
    }

    private static string? DetectUnsupportedUnit(string trimmed)
    {
        // Supported units: px, pt
        // Check for common unsupported units: em, rem, %, in, cm, mm, vh, vw, etc.
        if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase) ||
            trimmed.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            return null; // Supported unit
        }

        // Check for unit suffixes (2-3 characters at the end)
        if (trimmed.Length >= 3)
        {
            var lastTwo = trimmed.Substring(trimmed.Length - 2, 2).ToLowerInvariant();
            var lastThree = trimmed.Length >= 4 ? trimmed.Substring(trimmed.Length - 3, 3).ToLowerInvariant() : null;

            // Common unsupported units
            if (lastTwo == "em" || lastTwo == "in" || lastTwo == "cm" || lastTwo == "mm" ||
                lastTwo == "vh" || lastTwo == "vw" || lastTwo == "ex" || lastTwo == "ch" ||
                lastTwo == "pc" || trimmed.EndsWith("%"))
            {
                return lastTwo == "%" ? "%" : lastTwo;
            }

            if (lastThree == "rem")
            {
                return lastThree;
            }
        }

        return null; // No unit detected or unit is not recognized as unsupported
    }

    private static BorderLineStyle ParseBorderStyle(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return BorderLineStyle.None;
        }

        var token = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()
            ?.ToLowerInvariant();

        return token switch
        {
            HtmlCssConstants.CssValues.None => BorderLineStyle.None,
            HtmlCssConstants.CssValues.Solid => BorderLineStyle.Solid,
            HtmlCssConstants.CssValues.Dashed => BorderLineStyle.Dashed,
            HtmlCssConstants.CssValues.Dotted => BorderLineStyle.Dotted,
            _ => BorderLineStyle.None
        };
    }

    private static ColorRgba? ParseBorderColor(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }
        return ColorRgba.FromCss(raw.Trim(), ColorRgba.Black);
    }
}
