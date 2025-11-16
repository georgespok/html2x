using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Computes simplified CSS styles for supported HTML tags using AngleSharp's computed style API.
/// </summary>
public sealed class CssStyleComputer(
    IStyleTraversal traversal,
    IUserAgentDefaults uaDefaults,
    ICssValueConverter converter,
    IDiagnosticSession? diagnosticSession = null)
    : IStyleComputer
{
    private readonly IStyleTraversal _traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
    private readonly IUserAgentDefaults _uaDefaults = uaDefaults ?? throw new ArgumentNullException(nameof(uaDefaults));
    private readonly ICssValueConverter _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    private readonly IDiagnosticSession? _diagnosticSession = diagnosticSession;

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

        var style = new ComputedStyle();

        ApplyTypography(css, style, parentStyle);
        ApplySpacing(css, element, style);

        _uaDefaults.Apply(element, style, parentStyle);
        ApplyBorders(css, style);
        return style;
    }

    private void ApplyPageMargins(StyleTree tree, ICssStyleDeclaration styles, IElement element)
    {
        ApplySpacingWithOverrides(
            styles,
            HtmlCssConstants.CssProperties.Margin,
            HtmlCssConstants.CssProperties.MarginTop,
            HtmlCssConstants.CssProperties.MarginRight,
            HtmlCssConstants.CssProperties.MarginBottom,
            HtmlCssConstants.CssProperties.MarginLeft,
            allowNegative: true,
            element,
            value => tree.Page.MarginTopPt = value,
            value => tree.Page.MarginRightPt = value,
            value => tree.Page.MarginBottomPt = value,
            value => tree.Page.MarginLeftPt = value);
    }

    private void ApplySpacingWithOverrides(
        ICssStyleDeclaration css,
        string shorthandProperty,
        string topProperty,
        string rightProperty,
        string bottomProperty,
        string leftProperty,
        bool allowNegative,
        IElement element,
        Action<float> setTop,
        Action<float> setRight,
        Action<float> setBottom,
        Action<float> setLeft)
    {
        ApplySpacingShorthand(css, shorthandProperty, element, setTop, setRight, setBottom, setLeft, allowNegative);
        OverrideSpacingSide(css, topProperty, element, setTop, allowNegative);
        OverrideSpacingSide(css, rightProperty, element, setRight, allowNegative);
        OverrideSpacingSide(css, bottomProperty, element, setBottom, allowNegative);
        OverrideSpacingSide(css, leftProperty, element, setLeft, allowNegative);
    }

    private void OverrideSpacingSide(
        ICssStyleDeclaration css,
        string property,
        IElement element,
        Action<float> setter,
        bool allowNegative)
    {
        var raw = css.GetPropertyValue(property);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        setter(GetSpacingWithLogging(css, property, element, allowNegative));
    }

    private void ApplySpacingShorthand(
        ICssStyleDeclaration css,
        string shorthandProperty,
        IElement element,
        Action<float> setTop,
        Action<float> setRight,
        Action<float> setBottom,
        Action<float> setLeft,
        bool allowNegative)
    {
        var shorthandValue = css.GetPropertyValue(shorthandProperty);
        if (string.IsNullOrWhiteSpace(shorthandValue))
        {
            return;
        }

        if (!TryParseSpacingValues(shorthandProperty, shorthandValue, element, allowNegative, out var parsedValues))
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
                StyleLog.InvalidSpacingValue(_diagnosticSession, shorthandProperty, shorthandValue, element);
                break;
        }
    }

    private void ApplyTypography(ICssStyleDeclaration css, ComputedStyle style, ComputedStyle? parentStyle)
    {
        style.FontFamily = _converter.GetString(css, HtmlCssConstants.CssProperties.FontFamily,
            parentStyle?.FontFamily ?? HtmlCssConstants.Defaults.FontFamily);

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

        style.Color = _converter.GetString(css, HtmlCssConstants.CssProperties.Color,
            parentStyle?.Color ?? HtmlCssConstants.Defaults.Color);
    }

    private void ApplySpacing(ICssStyleDeclaration css, IElement element, ComputedStyle style)
    {
        ApplySpacingWithOverrides(
            css,
            HtmlCssConstants.CssProperties.Margin,
            HtmlCssConstants.CssProperties.MarginTop,
            HtmlCssConstants.CssProperties.MarginRight,
            HtmlCssConstants.CssProperties.MarginBottom,
            HtmlCssConstants.CssProperties.MarginLeft,
            allowNegative: true,
            element,
            value => style.MarginTopPt = value,
            value => style.MarginRightPt = value,
            value => style.MarginBottomPt = value,
            value => style.MarginLeftPt = value);

        ApplySpacingWithOverrides(
            css,
            HtmlCssConstants.CssProperties.Padding,
            HtmlCssConstants.CssProperties.PaddingTop,
            HtmlCssConstants.CssProperties.PaddingRight,
            HtmlCssConstants.CssProperties.PaddingBottom,
            HtmlCssConstants.CssProperties.PaddingLeft,
            allowNegative: false,
            element,
            value => style.PaddingTopPt = value,
            value => style.PaddingRightPt = value,
            value => style.PaddingBottomPt = value,
            value => style.PaddingLeftPt = value);
    }

    private bool TryParseSpacingValues(
        string property,
        string shorthandValue,
        IElement element,
        bool allowNegative,
        out List<float> parsedValues)
    {
        parsedValues = [];

        var tokens = shorthandValue.Split([' '], StringSplitOptions.RemoveEmptyEntries)
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
            StyleLog.UnsupportedSpacingUnit(_diagnosticSession, property, shorthandValue, unsupportedUnit, element);
                return false;
            }

        if (!_converter.TryGetLengthPt(token, out var points))
        {
            StyleLog.InvalidSpacingValue(_diagnosticSession, property, shorthandValue, element);
                return false;
            }

            if (points < 0 && !allowNegative)
            {
                StyleLog.NegativeSpacingValue(_diagnosticSession, property, points, element);
                return false;
            }

            parsedValues.Add(points);
        }

        return true;
    }

    private void ApplyBorders(ICssStyleDeclaration css, ComputedStyle style)
    {
        var hasWidth = _converter.TryGetLengthPt(css.GetPropertyValue(HtmlCssConstants.CssProperties.BorderWidth), out var widthPt);
        var lineStyle = ParseBorderStyle(css.GetPropertyValue(HtmlCssConstants.CssProperties.BorderStyle));

        if (!hasWidth || widthPt <= 0 || lineStyle == BorderLineStyle.None)
        {
            style.Borders = BorderEdges.None;
            return;
        }

        var color = ColorRgba.FromCss(style.Color, new ColorRgba(0, 0, 0, 255));
        var side = new BorderSide(widthPt, color, lineStyle);
        style.Borders = BorderEdges.Uniform(side);
    }

    private float GetSpacingWithLogging(ICssStyleDeclaration css, string property, IElement element, bool allowNegative)
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
            StyleLog.UnsupportedSpacingUnit(_diagnosticSession, property, rawValue, unsupportedUnit, element);
            return 0;
        }

        // Try to parse the value
        if (!_converter.TryGetLengthPt(rawValue, out var points))
        {
            // Value provided but couldn't be parsed (non-numeric, etc.)
            StyleLog.InvalidSpacingValue(_diagnosticSession, property, rawValue, element);
            return 0;
        }

        // Check for negative values
        if (points < 0 && !allowNegative)
        {
            StyleLog.NegativeSpacingValue(_diagnosticSession, property, points, element);
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
}
