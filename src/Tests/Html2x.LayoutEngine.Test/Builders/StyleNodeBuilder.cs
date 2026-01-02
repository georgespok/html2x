using AngleSharp.Dom;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Test.Builders;

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
        node.Style = node.Style with { Borders = borders };
        return this;
    }

    public StyleNodeBuilder WithPadding(float top, float right, float bottom, float left)
    {
        node.Style = node.Style with
        {
            Padding = new Spacing(top, right, bottom, left)
        };
        return this;
    }

    private static StyleNode CreateNode(IElement el, float? marginTop = null, float? marginLeft = null, float fontSize = 12)
    {
        var style = new ComputedStyle { FontSizePt = fontSize };
        var top = marginTop ?? 0f;
        var left = marginLeft ?? 0f;
        if (marginTop.HasValue || marginLeft.HasValue)
        {
            style = style with
            {
                Margin = new Spacing(top, 0f, 0f, left)
            };
        }

        return new StyleNode { Element = el, Style = style };
    }
}
