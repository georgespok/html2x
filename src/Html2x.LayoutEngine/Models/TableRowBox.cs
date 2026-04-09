namespace Html2x.LayoutEngine.Models;

public sealed class TableRowBox(DisplayRole role) : BlockBox(role)
{
    public int RowIndex { get; set; } = -1;
}
