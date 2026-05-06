using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Test.Builders;

internal sealed class StyleNodeBuilder(StyledElementFacts element, ComputedStyle style)
{
    private readonly List<StyleNodeBuilder> _children = [];
    private readonly List<ContentEntry> _content = [];

    private ComputedStyle _style = style ?? throw new ArgumentNullException(nameof(style));

    public StyleNodeBuilder AddText(string text)
    {
        _content.Add(ContentEntry.ForText(text));
        return this;
    }

    public StyleNodeBuilder AddLineBreak()
    {
        _content.Add(ContentEntry.ForLineBreak());
        return this;
    }

    public StyleNodeBuilder AddChild(
        string tagName,
        string? text = null,
        float? marginTop = null,
        float? marginLeft = null,
        float fontSize = 12)
    {
        var child = StyleTreeBuilder.CreateNodeBuilder(
            StyledElementFacts.Create(tagName),
            marginTop,
            marginLeft,
            fontSize);
        if (text is not null)
        {
            child.AddText(text);
        }

        AddChild(child);
        return this;
    }

    public StyleNodeBuilder AddChild(
        string tagName,
        Action<StyleNodeBuilder> configure,
        float? marginTop = null,
        float? marginLeft = null,
        float fontSize = 12)
    {
        var child = StyleTreeBuilder.CreateNodeBuilder(
            StyledElementFacts.Create(tagName),
            marginTop,
            marginLeft,
            fontSize);
        AddChild(child);
        configure(child);
        return this;
    }

    public StyleNodeBuilder WithBorders(BorderEdges borders)
    {
        _style = _style with { Borders = borders };
        return this;
    }

    public StyleNodeBuilder WithPadding(float top, float right, float bottom, float left)
    {
        _style = _style with
        {
            Padding = new(top, right, bottom, left)
        };
        return this;
    }

    internal StyleNode Build()
    {
        var builtChildren = _children
            .Select(static child => (Builder: child, Node: child.Build()))
            .ToArray();
        var children = builtChildren
            .Select(static item => item.Node)
            .ToArray();
        var content = _content
            .Select(entry => entry.ToStyleContent(builtChildren))
            .ToArray();

        return new(
            StyleSourceIdentity.Unspecified,
            element,
            _style,
            children,
            content);
    }

    internal void AddChild(StyleNodeBuilder child)
    {
        ArgumentNullException.ThrowIfNull(child);

        _children.Add(child);
        _content.Add(ContentEntry.ForChild(child));
    }

    private sealed record ContentEntry(string? Text, bool IsLineBreak, StyleNodeBuilder? Child)
    {
        public static ContentEntry ForText(string text) => new(text, false, null);

        public static ContentEntry ForLineBreak() => new(null, true, null);

        public static ContentEntry ForChild(StyleNodeBuilder child) => new(null, false, child);

        public StyleContentNode ToStyleContent(
            IReadOnlyList<(StyleNodeBuilder Builder, StyleNode Node)> builtChildren)
        {
            if (Text is not null)
            {
                return StyleContentNode.ForText(Text);
            }

            if (IsLineBreak)
            {
                return StyleContentNode.LineBreak;
            }

            if (Child is null)
            {
                throw new InvalidOperationException("Style content entry has no value.");
            }

            var child = builtChildren
                .FirstOrDefault(item => ReferenceEquals(item.Builder, Child))
                .Node;
            if (child is null)
            {
                throw new InvalidOperationException("Style child content was not built.");
            }

            return StyleContentNode.ForElement(child);
        }
    }
}