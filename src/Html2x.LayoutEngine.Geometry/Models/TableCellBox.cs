namespace Html2x.LayoutEngine.Models;

internal sealed class TableCellBox(BoxRole role) : BlockBox(role)
{
    public int ColumnIndex { get; set; } = -1;

    public bool IsHeader { get; set; }

    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return CopyBlockStateTo(new TableCellBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
            SourceIdentity = SourceIdentity,
            ColumnIndex = ColumnIndex,
            IsHeader = IsHeader
        });
    }
}
