using System.Drawing;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;
using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Box;

public sealed class TableLayoutEngine : ITableLayoutEngine
{
    private const float DefaultRowHeight = 20f;
    private readonly IInlineLayoutEngine _inlineEngine;
    private readonly BlockMeasurementService _measurement;
    private readonly IImageLayoutResolver _imageResolver;

    public TableLayoutEngine()
        : this(new InlineLayoutEngine(), new ImageLayoutResolver())
    {
    }

    internal TableLayoutEngine(IInlineLayoutEngine inlineEngine, IImageLayoutResolver? imageResolver = null)
    {
        _inlineEngine = inlineEngine ?? throw new ArgumentNullException(nameof(inlineEngine));
        _measurement = new BlockMeasurementService();
        _imageResolver = imageResolver ?? new ImageLayoutResolver();
    }

    public TableLayoutResult Layout(TableBox table, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(table);

        var requestedWidth = table.Style.WidthPt;
        var resolvedWidth = requestedWidth ?? Math.Max(0f, availableWidth);
        var validation = ValidateStructure(table);
        if (!validation.IsSupported)
        {
            return TableLayoutResult.Unsupported(
                requestedWidth,
                resolvedWidth,
                validation.StructureKind,
                validation.Reason,
                rowCount: CountRowsForDiagnostics(table));
        }

        var rows = ExtractSectionAwareRows(table).ToList();
        var derivedColumnCount = rows.Count == 0
            ? 0
            : rows.Max(static row => row.Children.OfType<TableCellBox>().Count());
        var columnWidths = BuildEqualColumnWidths(resolvedWidth, derivedColumnCount);
        var rowResults = BuildRowPlacements(rows, columnWidths);

        return new TableLayoutResult
        {
            RequestedWidth = requestedWidth,
            ResolvedWidth = resolvedWidth,
            RowCount = rowResults.Count,
            DerivedColumnCount = derivedColumnCount,
            ColumnWidths = columnWidths,
            Rows = rowResults,
            Height = rowResults.Count == 0
                ? DefaultRowHeight
                : rowResults.Max(static row => row.Y + row.Height)
        };
    }

    private static int CountRowsForDiagnostics(TableBox table)
    {
        return table.Children.Sum(static child => child switch
        {
            TableRowBox => 1,
            TableSectionBox section => section.Children.OfType<TableRowBox>().Count(),
            _ => 0
        });
    }

    private static TableStructureValidationResult ValidateStructure(TableBox table)
    {
        var hasDirectRows = false;
        var hasDirectSections = false;

        foreach (var child in table.Children)
        {
            switch (child)
            {
                case TableRowBox row:
                    hasDirectRows = true;
                    {
                        var rowValidation = ValidateRow(row);
                        if (!rowValidation.IsSupported)
                        {
                            return rowValidation;
                        }
                    }
                    break;
                case TableSectionBox section:
                    hasDirectSections = true;
                    {
                        var sectionValidation = ValidateSection(section);
                        if (!sectionValidation.IsSupported)
                        {
                            return sectionValidation;
                        }
                    }
                    break;
                default:
                    return TableStructureValidationResult.Unsupported(
                        "unsupported-table-child",
                        $"Tables currently support only direct row and section children. Found '{child.Role}'.");
            }
        }

        if (hasDirectRows && hasDirectSections)
        {
            return TableStructureValidationResult.Unsupported(
                "malformed-section-nesting",
                "Tables cannot mix direct rows with explicit table sections.");
        }

        return TableStructureValidationResult.Supported();
    }

    // Allowed hierarchy for this feature:
    // table -> tr -> td|th
    // table -> thead|tbody|tfoot -> tr -> td|th
    // Only direct tr children of table and direct tr children of direct section
    // children participate in the table row model. Arbitrary descendants do not.
    private static IEnumerable<TableRowBox> ExtractSectionAwareRows(TableBox table)
    {
        foreach (var child in table.Children)
        {
            if (child is TableRowBox row)
            {
                yield return row;
                continue;
            }

            if (child is TableSectionBox section)
            {
                foreach (var sectionRow in section.Children.OfType<TableRowBox>())
                {
                    yield return sectionRow;
                }
            }
        }
    }

    private static TableStructureValidationResult ValidateSection(TableSectionBox section)
    {
        foreach (var child in section.Children)
        {
            if (child is TableSectionBox)
            {
                return TableStructureValidationResult.Unsupported(
                    "malformed-section-nesting",
                    "Table sections cannot contain nested table sections.");
            }

            if (child is not TableRowBox row)
            {
                return TableStructureValidationResult.Unsupported(
                    "malformed-section-nesting",
                    $"Table sections currently support only direct row children. Found '{child.Role}'.");
            }

            var rowValidation = ValidateRow(row);
            if (!rowValidation.IsSupported)
            {
                return rowValidation;
            }
        }

        return TableStructureValidationResult.Supported();
    }

    private static TableStructureValidationResult ValidateRow(TableRowBox row)
    {
        foreach (var child in row.Children)
        {
            if (child is not TableCellBox cell)
            {
                return TableStructureValidationResult.Unsupported(
                    "unsupported-row-child",
                    $"Table rows currently support only direct cell children. Found '{child.Role}'.");
            }

            var cellValidation = ValidateCell(cell);
            if (!cellValidation.IsSupported)
            {
                return cellValidation;
            }
        }

        return TableStructureValidationResult.Supported();
    }

    private static TableStructureValidationResult ValidateCell(TableCellBox cell)
    {
        if (TryGetSpanValue(cell.Element, HtmlCssConstants.HtmlAttributes.Colspan, out var colspan) && colspan != 1)
        {
            return TableStructureValidationResult.Unsupported(
                HtmlCssConstants.HtmlAttributes.Colspan,
                "Table cell colspan is not supported.");
        }

        if (TryGetSpanValue(cell.Element, HtmlCssConstants.HtmlAttributes.Rowspan, out var rowspan) && rowspan != 1)
        {
            return TableStructureValidationResult.Unsupported(
                HtmlCssConstants.HtmlAttributes.Rowspan,
                "Table cell rowspan is not supported.");
        }

        return TableStructureValidationResult.Supported();
    }

    private static bool TryGetSpanValue(IElement? element, string attributeName, out int value)
    {
        value = 0;
        if (element is null || !element.HasAttribute(attributeName))
        {
            return false;
        }

        var rawValue = element.GetAttribute(attributeName);
        if (!int.TryParse(rawValue, out value) || value < 1)
        {
            value = 0;
            return true;
        }

        return true;
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
            var row = rows[rowIndex];
            var cells = row.Children.OfType<TableCellBox>().ToList();
            var rowHeight = Math.Max(
                DefaultRowHeight,
                cells
                    .Select((cell, columnIndex) => MeasureTableCellHeight(
                        cell,
                        columnIndex < columnWidths.Count ? columnWidths[columnIndex] : 0f))
                    .DefaultIfEmpty(DefaultRowHeight)
                    .Max());
            var placements = new List<TableLayoutCellPlacement>(cells.Count);
            var currentX = 0f;

            for (var columnIndex = 0; columnIndex < cells.Count; columnIndex++)
            {
                var sourceCell = cells[columnIndex];
                var width = columnIndex < columnWidths.Count ? columnWidths[columnIndex] : 0f;
                placements.Add(new TableLayoutCellPlacement
                {
                    SourceCell = sourceCell,
                    ColumnIndex = columnIndex,
                    IsHeader = string.Equals(sourceCell.Element?.TagName, "th", StringComparison.OrdinalIgnoreCase),
                    X = currentX,
                    Y = currentRowY,
                    Width = width,
                    Height = rowHeight,
                    UsedGeometry = CreateUsedGeometry(
                        currentX,
                        currentRowY,
                        width,
                        rowHeight,
                        sourceCell.Style.Padding.Safe(),
                        Spacing.FromBorderEdges(sourceCell.Style.Borders).Safe(),
                        markerOffset: sourceCell.MarkerOffset)
                });
                currentX += width;
            }

            results.Add(new TableLayoutRowResult
            {
                SourceRow = row,
                RowIndex = rowIndex,
                Y = currentRowY,
                Cells = placements,
                Height = rowHeight,
                UsedGeometry = CreateUsedGeometry(
                    0f,
                    currentRowY,
                    columnWidths.Sum(),
                    rowHeight,
                    row.Style.Padding.Safe(),
                    Spacing.FromBorderEdges(row.Style.Borders).Safe(),
                    markerOffset: row.MarkerOffset)
            });
            currentRowY += rowHeight;
        }

        return results;
    }

    private float MeasureTableCellHeight(TableCellBox cell, float assignedWidth)
    {
        var measurement = _measurement.Prepare(cell, assignedWidth);
        var inlineLayout = _inlineEngine.Layout(cell, InlineLayoutRequest.ForMeasurement(measurement.ContentWidth));
        var nestedHeight = MeasureStackedChildBlockHeights(cell.Children, measurement.ContentWidth);
        var contentHeight = _measurement.ResolveContentHeight(
            cell,
            Math.Max(inlineLayout.TotalHeight, nestedHeight));

        return Math.Max(0f, contentHeight + measurement.Padding.Vertical + measurement.Border.Vertical);
    }

    private float MeasureStackedChildBlockHeights(IEnumerable<DisplayNode> children, float availableWidth)
    {
        return _measurement.MeasureStackedChildBlocks(
            children,
            availableWidth,
            MeasureBlockLikeHeight,
            MeasureNestedTableHeight);
    }

    private float MeasureBlockLikeHeight(BlockBox block, float availableWidth)
    {
        if (block is ImageBox imageBox)
        {
            var imageMeasurement = _measurement.Prepare(imageBox, availableWidth);
            return _imageResolver.Resolve(imageBox, imageMeasurement.ContentWidth).TotalHeight;
        }

        if (block is RuleBox ruleBox)
        {
            var ruleMeasurement = _measurement.Prepare(ruleBox, availableWidth);
            return Math.Max(0f, ruleMeasurement.Padding.Vertical + ruleMeasurement.Border.Vertical);
        }

        var measurement = _measurement.Prepare(block, availableWidth);
        var inlineLayout = _inlineEngine.Layout(block, InlineLayoutRequest.ForMeasurement(measurement.ContentWidth));
        var nestedHeight = MeasureStackedChildBlockHeights(block.Children, measurement.ContentWidth);
        var contentHeight = _measurement.ResolveContentHeight(
            block,
            Math.Max(inlineLayout.TotalHeight, nestedHeight));

        return Math.Max(0f, contentHeight + measurement.Padding.Vertical + measurement.Border.Vertical);
    }

    private float MeasureNestedTableHeight(TableBox table, float availableWidth)
    {
        var measurement = _measurement.Prepare(table, availableWidth);
        return Layout(table, measurement.ResolvedWidth).Height;
    }

    private readonly record struct TableStructureValidationResult(bool IsSupported, string StructureKind, string Reason)
    {
        public static TableStructureValidationResult Supported()
        {
            return new TableStructureValidationResult(true, string.Empty, string.Empty);
        }

        public static TableStructureValidationResult Unsupported(string structureKind, string reason)
        {
            return new TableStructureValidationResult(false, structureKind, reason);
        }
    }

    private static UsedGeometry CreateUsedGeometry(
        float x,
        float y,
        float width,
        float height,
        Spacing padding,
        Spacing border,
        float? baseline = null,
        float markerOffset = 0f,
        bool allowsOverflow = false)
    {
        return UsedGeometry.FromBorderBox(
            new RectangleF(x, y, width, height),
            padding,
            border,
            baseline,
            markerOffset,
            allowsOverflow);
    }
}
