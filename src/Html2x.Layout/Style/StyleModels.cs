using AngleSharp.Dom;

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
    public string FontFamily { get; set; } = "Arial";
    public float FontSizePt { get; set; } = 12;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public string TextAlign { get; set; } = "left";
    public string Color { get; set; } = "#000000";
    public float MarginTopPt { get; set; }
    public float MarginRightPt { get; set; }
    public float MarginBottomPt { get; set; }
    public float MarginLeftPt { get; set; }
}