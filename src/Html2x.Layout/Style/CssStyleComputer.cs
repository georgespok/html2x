using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Core.Layout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Html2x.Layout.Style;

/// <summary>
/// Computes simplified CSS styles for supported HTML tags using AngleSharp's computed style API.
/// </summary>
public sealed class CssStyleComputer(
    IStyleTraversal traversal,
    IUserAgentDefaults uaDefaults,
    ICssValueConverter converter,
    ILogger<CssStyleComputer>? logger = null)
    : IStyleComputer
{
    private readonly IStyleTraversal _traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
    private readonly IUserAgentDefaults _uaDefaults = uaDefaults ?? throw new ArgumentNullException(nameof(uaDefaults));
    private readonly ICssValueConverter _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    private readonly ILogger<CssStyleComputer> _logger = logger ?? NullLogger<CssStyleComputer>.Instance;

    public CssStyleComputer()
        : this(new StyleTraversal(), new UserAgentDefaults(), new CssValueConverter(), null)
    {
    }

    public CssStyleComputer(IStyleTraversal traversal, IUserAgentDefaults uaDefaults)
        : this(traversal, uaDefaults, new CssValueConverter(), null)
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
        ApplyPageMargins(tree, bodyStyle);

        tree.Root = _traversal.Build(body, MapStyle);
        return tree;
    }

    private ComputedStyle MapStyle(IElement element, ComputedStyle? parentStyle)
    {
        var css = element.ComputeCurrentStyle();

        var style = new ComputedStyle
        {
            FontFamily = _converter.GetString(css, HtmlCssConstants.CssProperties.FontFamily,
                parentStyle?.FontFamily ?? HtmlCssConstants.Defaults.FontFamily),

            FontSizePt = _converter.TryGetLengthPt(css.GetPropertyValue(HtmlCssConstants.CssProperties.FontSize), out var fs)
                ? fs
                : parentStyle?.FontSizePt ?? HtmlCssConstants.Defaults.DefaultFontSizePt,

            Bold = _converter.IsBold(_converter.GetString(css, HtmlCssConstants.CssProperties.FontWeight)) ||
                   (parentStyle?.Bold ?? false),
            Italic = _converter.IsItalic(_converter.GetString(css, HtmlCssConstants.CssProperties.FontStyle)) ||
                     (parentStyle?.Italic ?? false),

            TextAlign = _converter.NormalizeAlign(
                _converter.GetString(css, HtmlCssConstants.CssProperties.TextAlign),
                parentStyle?.TextAlign ?? HtmlCssConstants.Defaults.TextAlign),

            Color = _converter.GetString(css, HtmlCssConstants.CssProperties.Color,
                parentStyle?.Color ?? HtmlCssConstants.Defaults.Color),

            MarginTopPt = _converter.GetLengthPt(css, HtmlCssConstants.CssProperties.MarginTop, 0),
            MarginRightPt = _converter.GetLengthPt(css, HtmlCssConstants.CssProperties.MarginRight, 0),
            MarginBottomPt = _converter.GetLengthPt(css, HtmlCssConstants.CssProperties.MarginBottom, 0),
            MarginLeftPt = _converter.GetLengthPt(css, HtmlCssConstants.CssProperties.MarginLeft, 0),

            PaddingTopPt = 0,
            PaddingRightPt = 0,
            PaddingBottomPt = 0,
            PaddingLeftPt = 0
        };

        // Apply padding shorthand first (if present)
        ApplyPaddingShorthand(css, style, element);

        // Then apply individual padding properties (override shorthand if explicitly set)
        var paddingTopValue = css.GetPropertyValue(HtmlCssConstants.CssProperties.PaddingTop);
        if (!string.IsNullOrWhiteSpace(paddingTopValue))
        {
            style.PaddingTopPt = GetPaddingWithLogging(css, HtmlCssConstants.CssProperties.PaddingTop, element);
        }

        var paddingRightValue = css.GetPropertyValue(HtmlCssConstants.CssProperties.PaddingRight);
        if (!string.IsNullOrWhiteSpace(paddingRightValue))
        {
            style.PaddingRightPt = GetPaddingWithLogging(css, HtmlCssConstants.CssProperties.PaddingRight, element);
        }

        var paddingBottomValue = css.GetPropertyValue(HtmlCssConstants.CssProperties.PaddingBottom);
        if (!string.IsNullOrWhiteSpace(paddingBottomValue))
        {
            style.PaddingBottomPt = GetPaddingWithLogging(css, HtmlCssConstants.CssProperties.PaddingBottom, element);
        }

        var paddingLeftValue = css.GetPropertyValue(HtmlCssConstants.CssProperties.PaddingLeft);
        if (!string.IsNullOrWhiteSpace(paddingLeftValue))
        {
            style.PaddingLeftPt = GetPaddingWithLogging(css, HtmlCssConstants.CssProperties.PaddingLeft, element);
        }

        _uaDefaults.Apply(element, style, parentStyle);
        ApplyBorders(css, style);
        return style;
    }

    private void ApplyPageMargins(StyleTree tree, ICssStyleDeclaration styles)
    {
        var shorthandDefined = _converter.TryGetLengthPt(styles.GetPropertyValue(HtmlCssConstants.CssProperties.Margin),
            out var shorthand);

        if (_converter.TryGetLengthPt(styles.GetPropertyValue(HtmlCssConstants.CssProperties.MarginTop), out var top))
        {
            tree.Page.MarginTopPt = top;
        }
        else if (shorthandDefined)
        {
            tree.Page.MarginTopPt = shorthand;
        }

        if (_converter.TryGetLengthPt(styles.GetPropertyValue(HtmlCssConstants.CssProperties.MarginRight), out var right))
        {
            tree.Page.MarginRightPt = right;
        }
        else if (shorthandDefined)
        {
            tree.Page.MarginRightPt = shorthand;
        }

        if (_converter.TryGetLengthPt(styles.GetPropertyValue(HtmlCssConstants.CssProperties.MarginBottom), out var bottom))
        {
            tree.Page.MarginBottomPt = bottom;
        }
        else if (shorthandDefined)
        {
            tree.Page.MarginBottomPt = shorthand;
        }

        if (_converter.TryGetLengthPt(styles.GetPropertyValue(HtmlCssConstants.CssProperties.MarginLeft), out var left))
        {
            tree.Page.MarginLeftPt = left;
        }
        else if (shorthandDefined)
        {
            tree.Page.MarginLeftPt = shorthand;
        }
    }

    private void ApplyPaddingShorthand(ICssStyleDeclaration css, ComputedStyle style, IElement element)
    {
        var shorthandValue = css.GetPropertyValue(HtmlCssConstants.CssProperties.Padding);
        if (string.IsNullOrWhiteSpace(shorthandValue))
        {
            return; // No shorthand defined
        }

        // Split shorthand value by spaces to get individual values
        var values = shorthandValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();

        if (values.Length == 0)
        {
            return; // Empty shorthand
        }

        // Parse values to points
        var parsedValues = new List<float>();
        foreach (var value in values)
        {
            // Check for unsupported units
            var unsupportedUnit = DetectUnsupportedUnit(value);
            if (unsupportedUnit != null)
            {
                StyleLog.UnsupportedPaddingUnit(_logger, HtmlCssConstants.CssProperties.Padding, shorthandValue, unsupportedUnit, element);
                return; // Invalid shorthand, don't apply
            }

            if (!_converter.TryGetLengthPt(value, out var points))
            {
                StyleLog.InvalidPaddingValue(_logger, HtmlCssConstants.CssProperties.Padding, shorthandValue, element);
                return; // Invalid shorthand, don't apply
            }

            if (points < 0)
            {
                StyleLog.NegativePaddingValue(_logger, HtmlCssConstants.CssProperties.Padding, points, element);
                return; // Invalid shorthand, don't apply
            }

            parsedValues.Add(points);
        }

        // Expand based on value count (CSS Box Model specification)
        switch (parsedValues.Count)
        {
            case 1:
                // All sides = value
                style.PaddingTopPt = parsedValues[0];
                style.PaddingRightPt = parsedValues[0];
                style.PaddingBottomPt = parsedValues[0];
                style.PaddingLeftPt = parsedValues[0];
                break;

            case 2:
                // top/bottom = first, left/right = second
                style.PaddingTopPt = parsedValues[0];
                style.PaddingRightPt = parsedValues[1];
                style.PaddingBottomPt = parsedValues[0];
                style.PaddingLeftPt = parsedValues[1];
                break;

            case 3:
                // top = first, left/right = second, bottom = third
                style.PaddingTopPt = parsedValues[0];
                style.PaddingRightPt = parsedValues[1];
                style.PaddingBottomPt = parsedValues[2];
                style.PaddingLeftPt = parsedValues[1];
                break;

            case 4:
                // top = first, right = second, bottom = third, left = fourth
                style.PaddingTopPt = parsedValues[0];
                style.PaddingRightPt = parsedValues[1];
                style.PaddingBottomPt = parsedValues[2];
                style.PaddingLeftPt = parsedValues[3];
                break;

            default:
                // More than 4 values is invalid
                StyleLog.InvalidPaddingValue(_logger, HtmlCssConstants.CssProperties.Padding, shorthandValue, element);
                break;
        }
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

    private float GetPaddingWithLogging(ICssStyleDeclaration css, string property, IElement element)
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
            StyleLog.UnsupportedPaddingUnit(_logger, property, rawValue, unsupportedUnit, element);
            return 0;
        }

        // Try to parse the value
        if (!_converter.TryGetLengthPt(rawValue, out var points))
        {
            // Value provided but couldn't be parsed (non-numeric, etc.)
            StyleLog.InvalidPaddingValue(_logger, property, rawValue, element);
            return 0;
        }

        // Check for negative values
        if (points < 0)
        {
            StyleLog.NegativePaddingValue(_logger, property, points, element);
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
