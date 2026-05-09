using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Geometry.Models;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Tables;

/// <summary>
///     Produces the supported table grid row and cell geometry model.
/// </summary>
internal sealed class TableGridLayout
{
    private const float DefaultRowHeight = 20f;
    private readonly TableCellMeasurement _cellMeasurement;
    private readonly BlockSizingRules _sizingRules;

    public TableGridLayout()
        : this(new(), new ImageSizingRules())
    {
    }

    internal TableGridLayout(InlineFlowLayout inlineFlowLayout, ImageSizingRules? imageResolver = null)
    {
        ArgumentNullException.ThrowIfNull(inlineFlowLayout);
        _sizingRules = new();
        _cellMeasurement = new(
            new(
                inlineFlowLayout,
                _sizingRules,
                imageResolver ?? new ImageSizingRules()));
    }

    public TableLayoutResult Layout(TableBox table, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(table);

        var measurement = _sizingRules.Prepare(table, availableWidth);
        var requestedWidth = table.Style.WidthPt;
        var resolvedWidth = measurement.BorderBoxWidth;
        var contentWidth = measurement.ContentFlowWidth;
        var rowModel = TableStructure.Build(table);
        if (!rowModel.IsSupported)
        {
            return TableLayoutResult.Unsupported(
                requestedWidth,
                resolvedWidth,
                rowModel.UnsupportedStructureKind,
                rowModel.UnsupportedReason,
                rowModel.RowCount);
        }

        var derivedColumnCount = rowModel.Rows.Count == 0
            ? 0
            : rowModel.Rows.Max(static row => row.EffectiveColumnCount);
        var columnWidths = BuildEqualColumnWidths(contentWidth, derivedColumnCount);
        var rowResults = BuildRowPlacements(rowModel.Rows, columnWidths);
        var contentHeight = rowResults.Count == 0
            ? 0f
            : rowResults.Max(static row => row.UsedGeometry.Y + row.UsedGeometry.Height);

        return new()
        {
            RequestedWidth = requestedWidth,
            ResolvedWidth = resolvedWidth,
            RowCount = rowResults.Count,
            DerivedColumnCount = derivedColumnCount,
            ColumnWidths = columnWidths,
            Rows = rowResults,
            ContentHeight = contentHeight,
            BorderBoxHeight = UsedGeometryRules.ResolveBorderBoxHeight(
                contentHeight,
                measurement.Padding,
                measurement.Border)
        };
    }

    private static IReadOnlyList<float> BuildEqualColumnWidths(float resolvedWidth, int derivedColumnCount)
    {
        if (derivedColumnCount <= 0)
        {
            return [];
        }

        var widthPerColumn = resolvedWidth / derivedColumnCount;
        return Enumerable.Repeat(widthPerColumn, derivedColumnCount).ToList();
    }

    private IReadOnlyList<TableLayoutRowResult> BuildRowPlacements(
        IReadOnlyList<TableRowFacts> rows,
        IReadOnlyList<float> columnWidths)
    {
        var results = new List<TableLayoutRowResult>(rows.Count);
        var currentRowY = 0f;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var placement = BuildRowPlacement(rows[rowIndex], rowIndex, currentRowY, columnWidths);
            results.Add(placement.Row);
            currentRowY += placement.BorderBoxHeight;
        }

        return results;
    }

    private RowPlacementBuildResult BuildRowPlacement(
        TableRowFacts row,
        int rowIndex,
        float rowY,
        IReadOnlyList<float> tableColumnWidths)
    {
        var rowPadding = row.SourceRow.Style.Padding.Safe();
        var rowBorder = Spacing.FromBorderEdges(row.SourceRow.Style.Borders).Safe();
        var rowBorderWidth = tableColumnWidths.Sum();
        var rowContentWidth = ResolveRowContentFlowWidth(row.SourceRow, rowBorderWidth, rowPadding, rowBorder);
        var rowColumnWidths = ScaleColumnWidths(tableColumnWidths, rowContentWidth);
        var rowContentHeight = MeasureRowContentHeight(row.Cells, rowColumnWidths);
        var rowGeometry = UsedGeometryRules.FromBorderBoxWithContentHeight(
            0f,
            rowY,
            rowBorderWidth,
            rowContentHeight,
            rowPadding,
            rowBorder,
            markerOffset: row.SourceRow.MarkerOffset);
        var rowContent = UsedGeometryRules.ResolveContentFlowArea(rowGeometry);
        var placements = BuildCellPlacements(row.Cells, rowColumnWidths, rowContent);
        var rowHeight = UsedGeometryRules.ResolveBorderBoxHeight(rowContentHeight, rowPadding, rowBorder);

        return new(
            new(row.SourceRow, rowIndex, rowGeometry, placements),
            rowHeight);
    }

    private static float ResolveRowContentFlowWidth(
        TableRowBox row,
        float rowBorderWidth,
        Spacing rowPadding,
        Spacing rowBorder) =>
        BoxDimensionRules.ResolveContentFlowWidth(
            rowBorderWidth,
            rowPadding,
            rowBorder,
            row.MarkerOffset);

    private float MeasureRowContentHeight(
        IReadOnlyList<TableCellFacts> cells,
        IReadOnlyList<float> rowColumnWidths)
    {
        var currentColumn = 0;
        var heights = new List<float>(cells.Count);
        foreach (var cell in cells)
        {
            var assignedWidth = SumColumnWidths(rowColumnWidths, currentColumn, cell.ColumnSpan);
            heights.Add(MeasureTableCellHeight(cell.SourceCell, assignedWidth));
            currentColumn += cell.ColumnSpan;
        }

        return Math.Max(
            DefaultRowHeight,
            heights.DefaultIfEmpty(DefaultRowHeight).Max());
    }

    private static IReadOnlyList<TableLayoutCellPlacement> BuildCellPlacements(
        IReadOnlyList<TableCellFacts> cells,
        IReadOnlyList<float> rowColumnWidths,
        ContentFlowArea rowContent)
    {
        var placements = new List<TableLayoutCellPlacement>(cells.Count);
        var currentX = rowContent.X;
        var currentColumn = 0;

        foreach (var cell in cells)
        {
            var width = SumColumnWidths(rowColumnWidths, currentColumn, cell.ColumnSpan);
            placements.Add(CreateCellPlacement(
                cell.SourceCell,
                currentColumn,
                cell.ColumnSpan,
                currentX,
                rowContent.Y,
                width,
                rowContent.Height));
            currentX += width;
            currentColumn += cell.ColumnSpan;
        }

        return placements;
    }

    private static TableLayoutCellPlacement CreateCellPlacement(
        TableCellBox sourceCell,
        int columnIndex,
        int columnSpan,
        float x,
        float y,
        float width,
        float height) =>
        new(
            sourceCell,
            columnIndex,
            columnSpan,
            HtmlElementRules.IsTableHeaderCell(sourceCell.Element),
            UsedGeometryRules.FromBorderBox(
                x,
                y,
                width,
                height,
                sourceCell.Style.Padding.Safe(),
                Spacing.FromBorderEdges(sourceCell.Style.Borders).Safe(),
                markerOffset: sourceCell.MarkerOffset));

    private static IReadOnlyList<float> ScaleColumnWidths(IReadOnlyList<float> columnWidths, float targetWidth)
    {
        if (columnWidths.Count == 0)
        {
            return [];
        }

        var sourceWidth = columnWidths.Sum();
        if (sourceWidth <= 0f)
        {
            return Enumerable.Repeat(0f, columnWidths.Count).ToList();
        }

        var scale = Math.Max(0f, targetWidth) / sourceWidth;
        return columnWidths.Select(width => width * scale).ToList();
    }

    private static float SumColumnWidths(IReadOnlyList<float> columnWidths, int startColumn, int columnSpan)
    {
        if (columnWidths.Count == 0 || columnSpan <= 0 || startColumn >= columnWidths.Count)
        {
            return 0f;
        }

        var width = 0f;
        var endColumn = Math.Min(columnWidths.Count, startColumn + columnSpan);
        for (var columnIndex = Math.Max(0, startColumn); columnIndex < endColumn; columnIndex++)
        {
            width += columnWidths[columnIndex];
        }

        return width;
    }

    private float MeasureTableCellHeight(TableCellBox cell, float assignedWidth) =>
        _cellMeasurement.MeasureContentHeight(cell, assignedWidth, MeasureNestedTable);

    private BlockContentSizeFacts MeasureNestedTable(TableBox table, float availableWidth)
    {
        var result = Layout(table, availableWidth);
        return BlockContentSizeFacts.ForTable(result);
    }

    private readonly record struct RowPlacementBuildResult(
        TableLayoutRowResult Row,
        float BorderBoxHeight);
}
