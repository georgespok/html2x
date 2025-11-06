using AngleSharp.Dom;
using Html2x.Core.Layout;

namespace Html2x.Layout.Style;

public sealed class StyleTree
{
    public StyleNode? Root { get; set; }
    public PageStyle Page { get; init; } = new();
}

public sealed class StyleNode
{
    public IElement Element { get; init; } = null!;
    public ComputedStyle Style { get; init; } = new();
    public List<StyleNode> Children { get; } = [];
}

public sealed class PageStyle
{
    public float MarginTopPt { get; set; } = 24;
    public float MarginRightPt { get; set; } = 24;
    public float MarginBottomPt { get; set; } = 24;
    public float MarginLeftPt { get; set; } = 24;
}

public sealed class ComputedStyle
{
    public string FontFamily { get; set; } = HtmlCssConstants.Defaults.FontFamily;
    public float FontSizePt { get; set; } = 12;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public string TextAlign { get; set; } = HtmlCssConstants.Defaults.TextAlign;
    public string Color { get; set; } = HtmlCssConstants.Defaults.Color;
    public float MarginTopPt { get; set; }
    public float MarginRightPt { get; set; }
    public float MarginBottomPt { get; set; }
    public float MarginLeftPt { get; set; }
    public BorderEdges Borders { get; set; } = BorderEdges.None;
}
