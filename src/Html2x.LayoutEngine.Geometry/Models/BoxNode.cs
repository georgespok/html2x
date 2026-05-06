namespace Html2x.LayoutEngine.Geometry.Models;

internal abstract class BoxNode(BoxRole role)
{
    public BoxNode? Parent { get; init; }
    public List<BoxNode> Children { get; } = [];
    public StyledElementFacts? Element { get; init; }
    public BoxRole Role { get; } = role;
    public ComputedStyle Style { get; set; } = new();
    public GeometrySourceIdentity SourceIdentity { get; init; } = GeometrySourceIdentity.Unspecified;

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