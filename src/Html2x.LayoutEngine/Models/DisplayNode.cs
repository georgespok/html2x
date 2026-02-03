using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Models;

public abstract class DisplayNode(DisplayRole role)
{
    public DisplayNode? Parent { get; init; }
    public List<DisplayNode> Children { get; } = [];
    public IElement? Element { get; init; }
    public DisplayRole Role { get; } = role;
    public ComputedStyle Style { get; set; } = new();
}
