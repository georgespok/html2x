namespace Html2x.LayoutEngine.Geometry.Models;

internal abstract class BoxNode(BoxRole role)
{
    private readonly List<BoxNode> _children = [];

    public BoxNode? Parent { get; init; }
    public IReadOnlyList<BoxNode> Children => _children;
    public StyledElementFacts? Element { get; init; }
    public BoxRole Role { get; } = role;
    public ComputedStyle Style { get; set; } = new();
    public GeometrySourceIdentity SourceIdentity { get; init; } = GeometrySourceIdentity.Unspecified;

    internal BoxNode CloneForParent(BoxNode parent)
    {
        var clone = CloneShallowForParent(parent);

        foreach (var child in Children)
        {
            clone.AddChild(child.CloneForParent(clone));
        }

        return clone;
    }

    internal void AddChild(BoxNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        _children.Add(child);
    }

    internal void InsertChild(int index, BoxNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        _children.Insert(index, child);
    }

    internal void AddChildren(IEnumerable<BoxNode> children)
    {
        ArgumentNullException.ThrowIfNull(children);

        foreach (var child in children)
        {
            AddChild(child);
        }
    }

    internal void ReplaceChildren(IEnumerable<BoxNode> children)
    {
        _children.Clear();
        AddChildren(children);
    }

    internal void ClearChildren() => _children.Clear();

    protected abstract BoxNode CloneShallowForParent(BoxNode parent);
}
