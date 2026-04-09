namespace Html2x.LayoutEngine.Models;

public sealed class TableCellBox(DisplayRole role) : BlockBox(role)
{
    public int ColumnIndex { get; set; } = -1;

    public bool IsHeader { get; set; }
}
