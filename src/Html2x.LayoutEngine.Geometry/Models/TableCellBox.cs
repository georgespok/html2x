namespace Html2x.LayoutEngine.Geometry.Models;

internal sealed class TableCellBox(BoxRole role) : BlockBox(role)
{
    public int ColumnIndex { get; set; } = -1;

    public int ColumnSpan { get; set; } = 1;

    public bool IsHeader { get; set; }

    internal void ApplyCellState(
        int columnIndex,
        int columnSpan,
        bool isHeader)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columnSpan);

        ColumnIndex = columnIndex;
        ColumnSpan = columnSpan;
        IsHeader = isHeader;
    }

    protected override BoxNode CloneShallowForParent(BoxNode parent) =>
        CopyBlockStateTo(new TableCellBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
            SourceIdentity = SourceIdentity,
            ColumnIndex = ColumnIndex,
            ColumnSpan = ColumnSpan,
            IsHeader = IsHeader
        });
}
