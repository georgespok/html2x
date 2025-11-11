using AngleSharp.Dom;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine.Test.Assertions;

internal sealed class StyleTreeBuilder(IElement body)
{
    private readonly StyleTree _tree = new()
    {
        Root = new StyleNode { Element = body, Style = new ComputedStyle() }
    };

    public StyleTreeBuilder WithPageMargins(float top, float right, float bottom, float left)
    {
        _tree.Page.MarginTopPt = top;
        _tree.Page.MarginRightPt = right;
        _tree.Page.MarginBottomPt = bottom;
        _tree.Page.MarginLeftPt = left;
        return this;
    }

    public StyleTreeBuilder AddChild(IElement element, float? marginTop = null, float? marginLeft = null, float fontSize = 12)
    {
        var node = CreateNode(element, marginTop, marginLeft, fontSize);
        _tree.Root!.Children.Add(node);
        return this;
    }

    public StyleTreeBuilder AddChild(IElement element, Action<StyleNodeBuilder> configure)
    {
        var node = CreateNode(element);
        configure(new StyleNodeBuilder(node));
        _tree.Root!.Children.Add(node);
        return this;
    }

    public StyleTree Build() => _tree;

    public static implicit operator StyleTree(StyleTreeBuilder builder) => builder._tree;

    private static StyleNode CreateNode(IElement el, float? marginTop = null, float? marginLeft = null, float fontSize = 12)
    {
        var style = new ComputedStyle { FontSizePt = fontSize };
        if (marginTop.HasValue)
        {
            style.MarginTopPt = marginTop.Value;
        }

        if (marginLeft.HasValue)
        {
            style.MarginLeftPt = marginLeft.Value;
        }

        return new StyleNode { Element = el, Style = style };
    }
}