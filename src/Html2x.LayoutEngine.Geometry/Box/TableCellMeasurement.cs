namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Measures table cell content without writing table or box layout state.
/// </summary>
internal sealed class TableCellMeasurement(BlockContentSizeMeasurement contentMeasurement)
{
    private readonly BlockContentSizeMeasurement _contentMeasurement =
        contentMeasurement ?? throw new ArgumentNullException(nameof(contentMeasurement));

    public float MeasureContentHeight(
        TableCellBox cell,
        float assignedWidth,
        Func<TableBox, float, BlockContentSizeFacts> measureNestedTable)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(measureNestedTable);

        return _contentMeasurement.Measure(cell, assignedWidth, measureNestedTable).BorderBoxHeight;
    }
}