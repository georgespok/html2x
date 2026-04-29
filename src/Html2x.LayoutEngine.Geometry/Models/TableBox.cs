namespace Html2x.LayoutEngine.Models;

internal sealed class TableBox(BoxRole role) : BlockBox(role)
{
    public int DerivedColumnCount { get; set; } = -1;

    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return CopyBlockStateTo(new TableBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
            SourceIdentity = SourceIdentity,
            DerivedColumnCount = DerivedColumnCount
        });
    }
}
