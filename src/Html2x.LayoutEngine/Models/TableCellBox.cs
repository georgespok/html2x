namespace Html2x.LayoutEngine.Models;

public sealed class TableCellBox(DisplayRole role) : BlockBox(role)
{
    public int ColumnIndex { get; set; } = -1;

    public bool IsHeader { get; set; }

    protected override DisplayNode CloneShallowForParent(DisplayNode parent)
    {
        return new TableCellBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Margin = Margin,
            Padding = Padding,
            TextAlign = TextAlign,
            MarkerOffset = MarkerOffset,
            UsedGeometry = UsedGeometry,
            IsAnonymous = IsAnonymous,
            IsInlineBlockContext = IsInlineBlockContext,
            ColumnIndex = ColumnIndex,
            IsHeader = IsHeader
        };
    }
}
