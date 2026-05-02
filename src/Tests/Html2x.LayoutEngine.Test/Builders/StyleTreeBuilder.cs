using Html2x.RenderModel;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Test.Builders;

internal sealed class StyleTreeBuilder
{
    private readonly PageStyle _page = new();
    private readonly StyleNodeBuilder _root;

    public StyleTreeBuilder(string rootTagName = HtmlCssConstants.HtmlTags.Body)
    {
        _root = CreateNodeBuilder(StyledElementFacts.Create(rootTagName));
    }

    public StyleTreeBuilder WithPageMargins(float top, float right, float bottom, float left)
    {
        _page.Margin = new Spacing(top, right, bottom, left);
        return this;
    }

    public StyleTreeBuilder AddChild(
        string tagName,
        string? text = null,
        float? marginTop = null,
        float? marginLeft = null,
        float fontSize = 12)
    {
        var child = CreateNodeBuilder(StyledElementFacts.Create(tagName), marginTop, marginLeft, fontSize);
        if (text is not null)
        {
            child.AddText(text);
        }

        _root.AddChild(child);
        return this;
    }

    public StyleTreeBuilder AddChild(
        string tagName,
        Action<StyleNodeBuilder> configure,
        float? marginTop = null,
        float? marginLeft = null,
        float fontSize = 12)
    {
        var child = CreateNodeBuilder(StyledElementFacts.Create(tagName), marginTop, marginLeft, fontSize);
        _root.AddChild(child);
        configure(child);
        return this;
    }

    public StyleTree Build() => new()
    {
        Root = _root.Build(),
        Page = _page
    };

    public static implicit operator StyleTree(StyleTreeBuilder builder) => builder.Build();

    internal static StyleNodeBuilder CreateNodeBuilder(
        StyledElementFacts element,
        float? marginTop = null,
        float? marginLeft = null,
        float fontSize = 12)
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

        return new StyleNodeBuilder(element, style);
    }
}
