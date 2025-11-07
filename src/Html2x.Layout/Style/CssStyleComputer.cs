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

            PaddingTopPt = GetPaddingWithLogging(css, HtmlCssConstants.CssProperties.PaddingTop, element),
            PaddingRightPt = GetPaddingWithLogging(css, HtmlCssConstants.CssProperties.PaddingRight, element),
            PaddingBottomPt = GetPaddingWithLogging(css, HtmlCssConstants.CssProperties.PaddingBottom, element),
            PaddingLeftPt = GetPaddingWithLogging(css, HtmlCssConstants.CssProperties.PaddingLeft, element)
        };

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
