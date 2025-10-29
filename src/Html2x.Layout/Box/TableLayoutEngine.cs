namespace Html2x.Layout.Box;

public sealed class TableLayoutEngine : ITableLayoutEngine
{
    public float MeasureHeight(TableBox table, float availableWidth)
    {
        // MVP: assume each row ≈ 20pt
        var rowCount = table.Children.OfType<TableRowBox>().Count();
        return Math.Max(20 * rowCount, 20);
    }
}