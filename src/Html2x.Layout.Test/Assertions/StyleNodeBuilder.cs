using AngleSharp.Dom;
using Html2x.Core.Layout;
using Html2x.Layout.Style;

namespace Html2x.Layout.Test.Assertions;

internal sealed class StyleNodeBuilder(StyleNode node)
{
    public StyleNodeBuilder AddChild(IElement element, float? marginTop = null, float? marginLeft = null, float fontSize = 12)
    {
        var child = CreateNode(element, marginTop, marginLeft, fontSize);
        node.Children.Add(child);
        return this;
    }

    public StyleNodeBuilder AddChild(IElement element, Action<StyleNodeBuilder> configure)
    {
        var child = CreateNode(element);
        configure(new StyleNodeBuilder(child));
        node.Children.Add(child);
        return this;
    }

    public StyleNodeBuilder WithBorders(BorderEdges borders)
    {
        node.Style.Borders = borders;
        return this;
    }

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