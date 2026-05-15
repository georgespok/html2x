namespace Html2x.LayoutEngine.Geometry.Models;

internal sealed class TableBox(BoxRole role) : BlockBox(role)
{
    public int DerivedColumnCount { get; set; } = -1;

    internal void ApplyTableState(int derivedColumnCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(derivedColumnCount);
        DerivedColumnCount = derivedColumnCount;
    }

    internal void ApplyUnsupportedPlaceholderState()
    {
        DerivedColumnCount = 0;
        ClearChildren();
    }

    protected override BoxNode CloneShallowForParent(BoxNode parent) =>
        CopyBlockStateTo(new TableBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
            SourceIdentity = SourceIdentity,
            DerivedColumnCount = DerivedColumnCount
        });
}
