using Html2x.RenderModel;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Computes the current supported table row and cell geometry model without adding advanced table features.
/// </summary>
internal sealed class TableLayoutEngine
{
    private const float DefaultRowHeight = 20f;
    private readonly BlockMeasurementService _measurement;
    private readonly BlockContentMeasurementService _contentMeasurement;

    public TableLayoutEngine()
        : this(new InlineLayoutEngine(), new ImageLayoutResolver())
    {
    }

    internal TableLayoutEngine(InlineLayoutEngine inlineEngine, IImageLayoutResolver? imageResolver = null)
    {
        ArgumentNullException.ThrowIfNull(inlineEngine);
        _measurement = new BlockMeasurementService();
        _contentMeasurement = new BlockContentMeasurementService(
            inlineEngine,
            _measurement,
            imageResolver ?? new ImageLayoutResolver());
    }

    public TableLayoutResult Layout(TableBox table, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(table);

        var measurement = _measurement.Prepare(table, availableWidth);
        var requestedWidth = table.Style.WidthPt;
        var resolvedWidth = measurement.BorderBoxWidth;
        var contentWidth = measurement.ContentFlowWidth;
        var rowModel = TableRowModel.Build(table);
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
            : rowModel.Rows.Max(static row => row.Children.OfType<TableCellBox>().Count());
        var columnWidths = BuildEqualColumnWidths(contentWidth, derivedColumnCount);
        var rowResults = BuildRowPlacements(rowModel.Rows, columnWidths);
        var contentHeight = rowResults.Count == 0
            ? 0f
            : rowResults.Max(static row => row.Y + row.Height);

        return new TableLayoutResult
        {
            RequestedWidth = requestedWidth,
            ResolvedWidth = resolvedWidth,
            RowCount = rowResults.Count,
            DerivedColumnCount = derivedColumnCount,
            ColumnWidths = columnWidths,
            Rows = rowResults,
            ContentHeight = contentHeight,
            BorderBoxHeight = BoxGeometryFactory.ResolveBorderBoxHeight(
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
        IReadOnlyList<TableRowBox> rows,
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
        TableRowBox row,
        int rowIndex,
        float rowY,
        IReadOnlyList<float> tableColumnWidths)
    {
        var cells = row.Children.OfType<TableCellBox>().ToList();
        var rowPadding = row.Style.Padding.Safe();
        var rowBorder = Spacing.FromBorderEdges(row.Style.Borders).Safe();
        var rowBorderWidth = tableColumnWidths.Sum();
        var rowContentWidth = ResolveRowContentFlowWidth(row, rowBorderWidth, rowPadding, rowBorder);
        var rowColumnWidths = ScaleColumnWidths(tableColumnWidths, rowContentWidth);
        var rowContentHeight = MeasureRowContentHeight(cells, rowColumnWidths);
        var rowGeometry = BoxGeometryFactory.FromBorderBoxWithContentHeight(
            0f,
            rowY,
            rowBorderWidth,
            rowContentHeight,
            rowPadding,
            rowBorder,
            markerOffset: row.MarkerOffset);
        var rowContent = BoxGeometryFactory.ResolveContentFlowArea(rowGeometry);
        var placements = BuildCellPlacements(cells, rowColumnWidths, rowContent);
        var rowHeight = BoxGeometryFactory.ResolveBorderBoxHeight(rowContentHeight, rowPadding, rowBorder);

        return new RowPlacementBuildResult(
            new TableLayoutRowResult(row, rowIndex, rowGeometry, placements),
            rowHeight);
    }

    private static float ResolveRowContentFlowWidth(
        TableRowBox row,
        float rowBorderWidth,
        Spacing rowPadding,
        Spacing rowBorder)
    {
        return BoxDimensionResolver.ResolveContentFlowWidth(
            rowBorderWidth,
            rowPadding,
            rowBorder,
            row.MarkerOffset);
    }

    private float MeasureRowContentHeight(
        IReadOnlyList<TableCellBox> cells,
        IReadOnlyList<float> rowColumnWidths)
    {
        return Math.Max(
            DefaultRowHeight,
            cells
                .Select((cell, columnIndex) => MeasureTableCellHeight(
                    cell,
                    columnIndex < rowColumnWidths.Count ? rowColumnWidths[columnIndex] : 0f))
                .DefaultIfEmpty(DefaultRowHeight)
                .Max());
    }

    private static IReadOnlyList<TableLayoutCellPlacement> BuildCellPlacements(
        IReadOnlyList<TableCellBox> cells,
        IReadOnlyList<float> rowColumnWidths,
        ContentFlowArea rowContent)
    {
        var placements = new List<TableLayoutCellPlacement>(cells.Count);
        var currentX = rowContent.X;

        for (var columnIndex = 0; columnIndex < cells.Count; columnIndex++)
        {
            var width = columnIndex < rowColumnWidths.Count ? rowColumnWidths[columnIndex] : 0f;
            placements.Add(CreateCellPlacement(
                cells[columnIndex],
                columnIndex,
                currentX,
                rowContent.Y,
                width,
                rowContent.Height));
            currentX += width;
        }

        return placements;
    }

    private static TableLayoutCellPlacement CreateCellPlacement(
        TableCellBox sourceCell,
        int columnIndex,
        float x,
        float y,
        float width,
        float height)
    {
        return new TableLayoutCellPlacement(
            sourceCell,
            columnIndex,
            HtmlElementClassifier.IsTableHeaderCell(sourceCell.Element),
            BoxGeometryFactory.FromBorderBox(
                x,
                y,
                width,
                height,
                sourceCell.Style.Padding.Safe(),
                Spacing.FromBorderEdges(sourceCell.Style.Borders).Safe(),
                markerOffset: sourceCell.MarkerOffset));
    }

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

    private float MeasureTableCellHeight(TableCellBox cell, float assignedWidth)
    {
        return _contentMeasurement.Measure(cell, assignedWidth, MeasureNestedTable).BorderBoxHeight;
    }

    private BlockContentMeasurement MeasureNestedTable(TableBox table, float availableWidth)
    {
        var result = Layout(table, availableWidth);
        return BlockContentMeasurement.ForTable(result);
    }

    private readonly record struct RowPlacementBuildResult(
        TableLayoutRowResult Row,
        float BorderBoxHeight);

}
