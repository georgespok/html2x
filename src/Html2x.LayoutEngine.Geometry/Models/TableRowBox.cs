namespace Html2x.LayoutEngine.Geometry.Models;

internal sealed class TableRowBox(BoxRole role) : BlockBox(role)
{
    public int RowIndex { get; set; } = -1;

    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return CopyBlockStateTo(new TableRowBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
            SourceIdentity = SourceIdentity,
            RowIndex = RowIndex
        });
    }
}
