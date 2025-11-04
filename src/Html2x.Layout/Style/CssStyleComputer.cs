using System;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Layout;

namespace Html2x.Layout.Style;

/// <summary>
/// Computes simplified CSS styles for supported HTML tags using AngleSharp's computed style API.
/// </summary>
public sealed class CssStyleComputer : IStyleComputer
{
    private readonly IStyleTraversal _traversal;
    private readonly IUserAgentDefaults _uaDefaults;
    private readonly ICssValueConverter _converter;

    public CssStyleComputer()
        : this(new StyleTraversal(), new UserAgentDefaults(), new CssValueConverter())
    {
    }

    public CssStyleComputer(IStyleTraversal traversal, IUserAgentDefaults uaDefaults)
        : this(traversal, uaDefaults, new CssValueConverter())
    {
    }

    public CssStyleComputer(IStyleTraversal traversal, IUserAgentDefaults uaDefaults, ICssValueConverter converter)
    {
        _traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
        _uaDefaults = uaDefaults ?? throw new ArgumentNullException(nameof(uaDefaults));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
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
}
