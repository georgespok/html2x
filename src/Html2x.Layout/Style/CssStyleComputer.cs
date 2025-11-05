using System.Linq;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Core.Layout;

namespace Html2x.Layout.Style;

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
            MarginLeftPt = _converter.GetLengthPt(css, HtmlCssConstants.CssProperties.MarginLeft, 0)
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
