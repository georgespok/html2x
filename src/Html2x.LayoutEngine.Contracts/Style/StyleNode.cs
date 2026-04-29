namespace Html2x.LayoutEngine.Models;

public sealed class StyleNode
{
    public StyleNode()
        : this(
            StyleSourceIdentity.Unspecified,
            StyledElementFacts.Empty,
            new ComputedStyle())
    {
    }

    public StyleNode(
        StyleSourceIdentity identity,
        StyledElementFacts element,
        ComputedStyle style,
        IEnumerable<StyleNode>? children = null,
        IEnumerable<StyleContentNode>? content = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(style);

        Identity = identity;
        Element = element;
        Style = style;
        Children = children?.ToArray() ?? [];
        Content = content?.ToArray() ?? [];
    }

    public StyleSourceIdentity Identity { get; init; } = StyleSourceIdentity.Unspecified;
    public StyledElementFacts Element { get; init; } = StyledElementFacts.Empty;
    public ComputedStyle Style { get; init; } = new();
    public IReadOnlyList<StyleNode> Children { get; }
    public IReadOnlyList<StyleContentNode> Content { get; }
}
