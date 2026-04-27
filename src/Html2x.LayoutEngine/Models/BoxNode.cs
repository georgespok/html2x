using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Models;

public abstract class BoxNode(BoxRole role)
{
    public BoxNode? Parent { get; init; }
    public List<BoxNode> Children { get; } = [];
    public IElement? Element { get; init; }
    public BoxRole Role { get; } = role;
    public ComputedStyle Style { get; set; } = new();

    internal BoxNode CloneForParent(BoxNode parent)
    {
        var clone = CloneShallowForParent(parent);

        foreach (var child in Children)
        {
            clone.Children.Add(child.CloneForParent(clone));
        }

        return clone;
    }

    protected abstract BoxNode CloneShallowForParent(BoxNode parent);
}
