using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Models;

public abstract class DisplayNode(DisplayRole role)
{
    public DisplayNode? Parent { get; init; }
    public List<DisplayNode> Children { get; } = [];
    public IElement? Element { get; init; }
    public DisplayRole Role { get; } = role;
    public ComputedStyle Style { get; set; } = new();

    internal DisplayNode CloneForParent(DisplayNode parent)
    {
        var clone = CloneShallowForParent(parent);

        foreach (var child in Children)
        {
            clone.Children.Add(child.CloneForParent(clone));
        }

        return clone;
    }

    protected abstract DisplayNode CloneShallowForParent(DisplayNode parent);
}
