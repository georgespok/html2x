using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.LayoutEngine.Geometry.Writing;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Tables;

/// <summary>
///     Writes table layout results back to table, row, and cell boxes before fragment tree building.
/// </summary>
internal sealed class TablePlacementWriter(LayoutBoxStateWriter stateWriter)
{
    private readonly LayoutBoxStateWriter _stateWriter =
        stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));

    public TablePlacementWriter()
        : this(new())
    {
    }

    public TableBox WriteSupported(
        TableBox table,
        TableLayoutResult result,
        float x,
        float y,
        Spacing margin,
        Func<BlockChildLayoutRequest, float> layoutChildBlocks)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(layoutChildBlocks);

        var padding = table.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(table.Style.Borders).Safe();
        var tableGeometry = UsedGeometryRules.FromBorderBoxWithContentHeight(
            x,
            y,
            result.ResolvedBorderBoxWidth,
            result.ContentHeight,
            padding,
            border,
            markerOffset: table.MarkerOffset);
        _stateWriter.ApplyTableLayout(
            table,
            margin,
            padding,
            tableGeometry,
            result.DerivedColumnCount);

        var tableContent = UsedGeometryRules.ResolveContentFlowArea(tableGeometry);
        foreach (var rowResult in result.Rows)
        {
            WriteTableRowLayout(rowResult, tableContent.X, tableContent.Y, layoutChildBlocks);
        }

        return table;
    }

    public TableBox WriteUnsupportedPlaceholder(
        TableBox sourceTable,
        float x,
        float y,
        float width,
        Spacing margin)
    {
        ArgumentNullException.ThrowIfNull(sourceTable);

        _stateWriter.ApplyUnsupportedTablePlaceholder(
            sourceTable,
            margin,
            UsedGeometryRules.FromBorderBox(
                x,
                y,
                width,
                0f,
                sourceTable.Style.Padding.Safe(),
                Spacing.FromBorderEdges(sourceTable.Style.Borders).Safe(),
                markerOffset: sourceTable.MarkerOffset));
        return sourceTable;
    }

    private void WriteTableRowLayout(
        TableLayoutRowResult rowResult,
        float tableX,
        float tableY,
        Func<BlockChildLayoutRequest, float> layoutChildBlocks)
    {
        var rowBlock = rowResult.SourceRow;
        _stateWriter.ApplyTableRowLayout(
            rowBlock,
            rowResult.RowIndex,
            GeometryTranslator.Translate(rowResult.UsedGeometry, tableX, tableY));

        foreach (var placement in rowResult.Cells)
        {
            WriteTableCellLayout(placement, tableX, tableY, layoutChildBlocks);
        }
    }

    private void WriteTableCellLayout(
        TableLayoutCellPlacement placement,
        float tableX,
        float tableY,
        Func<BlockChildLayoutRequest, float> layoutChildBlocks)
    {
        var cellBlock = placement.SourceCell;
        _stateWriter.ApplyTableCellLayout(
            cellBlock,
            placement.ColumnIndex,
            placement.ColumnSpan,
            placement.IsHeader,
            GeometryTranslator.Translate(placement.UsedGeometry, tableX, tableY));

        LayoutTableCellContent(cellBlock, layoutChildBlocks);
    }

    private void LayoutTableCellContent(
        TableCellBox cell,
        Func<BlockChildLayoutRequest, float> layoutChildBlocks)
    {
        var geometry = cell.UsedGeometry ?? throw new InvalidOperationException(
            "Table cell content layout requires UsedGeometry to be applied before child placement.");
        var contentArea = UsedGeometryRules.ResolveContentFlowArea(geometry);
        var contentX = contentArea.X;
        var contentY = contentArea.Y;
        var contentWidth = contentArea.Width;

        layoutChildBlocks(new(
            cell,
            contentX,
            contentY,
            contentWidth,
            contentY));
    }
}
