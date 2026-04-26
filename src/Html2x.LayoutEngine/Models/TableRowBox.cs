namespace Html2x.LayoutEngine.Models;

public sealed class TableRowBox(DisplayRole role) : BlockBox(role)
{
    public int RowIndex { get; set; } = -1;

    protected override DisplayNode CloneShallowForParent(DisplayNode parent)
    {
        return new TableRowBox(Role)
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
            RowIndex = RowIndex
        };
    }
}
