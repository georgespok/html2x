using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Applies table layout results back to table, row, and cell boxes for fragment projection compatibility.
/// </summary>
internal sealed class TablePlacementApplier
{
    public TableBox ApplySupported(
        TableBox table,
        TableLayoutResult result,
        float x,
        float y,
        Spacing margin,
        Func<BlockBox, float, float, float, float, float> layoutChildBlocks)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(layoutChildBlocks);

        table.Margin = margin;
        var padding = table.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(table.Style.Borders).Safe();
        table.Padding = padding;
        table.TextAlign = table.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        table.DerivedColumnCount = result.DerivedColumnCount;
        var tableGeometry = BoxGeometryFactory.FromBorderBoxWithContentHeight(
            x,
            y,
            result.ResolvedWidth,
            result.ContentHeight,
            padding,
            border,
            markerOffset: table.MarkerOffset);
        table.ApplyLayoutGeometry(tableGeometry);

        var tableContent = BoxGeometryFactory.ResolveContentFlowArea(tableGeometry);
        foreach (var rowResult in result.Rows)
        {
            ApplyTableRowLayout(rowResult, tableContent.X, tableContent.Y, layoutChildBlocks);
        }

        return table;
    }

    public static TableBox ApplyUnsupportedPlaceholder(
        TableBox sourceTable,
        float x,
        float y,
        float width,
        Spacing margin)
    {
        ArgumentNullException.ThrowIfNull(sourceTable);

        sourceTable.Margin = margin;
        sourceTable.Padding = sourceTable.Style.Padding.Safe();
        sourceTable.TextAlign = sourceTable.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        sourceTable.DerivedColumnCount = 0;
        sourceTable.ApplyLayoutGeometry(BoxGeometryFactory.FromBorderBox(
            x,
            y,
            width,
            0f,
            sourceTable.Style.Padding.Safe(),
            Spacing.FromBorderEdges(sourceTable.Style.Borders).Safe(),
            markerOffset: sourceTable.MarkerOffset));
        sourceTable.Children.Clear();
        return sourceTable;
    }

    private void ApplyTableRowLayout(
        TableLayoutRowResult rowResult,
        float tableX,
        float tableY,
        Func<BlockBox, float, float, float, float, float> layoutChildBlocks)
    {
        var rowBlock = rowResult.SourceRow;
        rowBlock.Margin = rowBlock.Style.Margin.Safe();
        rowBlock.Padding = rowBlock.Style.Padding.Safe();
        rowBlock.RowIndex = rowResult.RowIndex;
        rowBlock.TextAlign = rowBlock.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        rowBlock.ApplyLayoutGeometry(rowResult.UsedGeometry.Translate(tableX, tableY));

        foreach (var placement in rowResult.Cells)
        {
            ApplyTableCellLayout(placement, tableX, tableY, layoutChildBlocks);
        }
    }

    private void ApplyTableCellLayout(
        TableLayoutCellPlacement placement,
        float tableX,
        float tableY,
        Func<BlockBox, float, float, float, float, float> layoutChildBlocks)
    {
        var cellBlock = placement.SourceCell;
        cellBlock.Margin = cellBlock.Style.Margin.Safe();
        cellBlock.Padding = cellBlock.Style.Padding.Safe();
        cellBlock.ColumnIndex = placement.ColumnIndex;
        cellBlock.IsHeader = placement.IsHeader;
        cellBlock.TextAlign = cellBlock.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        cellBlock.ApplyLayoutGeometry(placement.UsedGeometry.Translate(tableX, tableY));

        LayoutTableCellContent(cellBlock, layoutChildBlocks);
    }

    private void LayoutTableCellContent(
        TableCellBox cell,
        Func<BlockBox, float, float, float, float, float> layoutChildBlocks)
    {
        var geometry = cell.UsedGeometry ?? throw new InvalidOperationException(
            "Table cell content layout requires UsedGeometry to be applied before child placement.");
        var contentArea = BoxGeometryFactory.ResolveContentFlowArea(geometry);
        var contentX = contentArea.X;
        var contentY = contentArea.Y;
        var contentWidth = contentArea.Width;

        layoutChildBlocks(
            cell,
            contentX,
            contentY,
            contentWidth,
            contentY);
    }
}
