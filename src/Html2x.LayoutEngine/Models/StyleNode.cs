using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Models;

public sealed class StyleNode
{
    public IElement Element { get; init; } = null!;
    public ComputedStyle Style { get; init; } = new();
    public List<StyleNode> Children { get; } = [];
}