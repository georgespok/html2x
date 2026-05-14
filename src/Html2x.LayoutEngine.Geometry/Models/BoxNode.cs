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
        ValidateChildForAdd(child, nameof(child));

        _children.Add(child);
    }

    internal void InsertChild(int index, BoxNode child)
    {
        if ((uint)index > (uint)_children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        ValidateChildForAdd(child, nameof(child));

        _children.Insert(index, child);
    }

    internal void AddChildren(IEnumerable<BoxNode> children)
    {
        var validatedChildren = ValidateChildrenForAdd(children);

        foreach (var child in validatedChildren)
        {
            _children.Add(child);
        }
    }

    internal void ReplaceChildren(IEnumerable<BoxNode> children)
    {
        var validatedChildren = ValidateChildrenForAdd(children);

        _children.Clear();
        _children.AddRange(validatedChildren);
    }

    internal void ClearChildren() => _children.Clear();

    protected abstract BoxNode CloneShallowForParent(BoxNode parent);

    private void ValidateChildForAdd(BoxNode? child, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(child, parameterName);

        if (child.Parent is not null && !ReferenceEquals(child.Parent, this))
        {
            throw new ArgumentException(
                "Child already belongs to a different parent.",
                parameterName);
        }
    }

    private BoxNode[] ValidateChildrenForAdd(IEnumerable<BoxNode> children)
    {
        ArgumentNullException.ThrowIfNull(children);

        var validatedChildren = children as BoxNode[] ?? children.ToArray();
        foreach (var child in validatedChildren)
        {
            if (child is null)
            {
                throw new ArgumentException(
                    "Child collection cannot contain null entries.",
                    nameof(children));
            }

            if (child.Parent is not null && !ReferenceEquals(child.Parent, this))
            {
                throw new ArgumentException(
                    "Child collection contains a child that already belongs to a different parent.",
                    nameof(children));
            }
        }

        return validatedChildren;
    }
}
