using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public sealed class BlockLayoutEngine
{
    private readonly IInlineLayoutEngine _inlineEngine;
    private readonly BlockMeasurementService _measurement;
    private readonly ITableLayoutEngine _tableEngine;
    private readonly IFloatLayoutEngine _floatEngine;
    private readonly DiagnosticsSession? _diagnosticsSession;
    private readonly IBlockFormattingContext _blockFormattingContext;

    public BlockLayoutEngine(
        IInlineLayoutEngine inlineEngine,
        ITableLayoutEngine tableEngine,
        IFloatLayoutEngine floatEngine,
        DiagnosticsSession? diagnosticsSession = null)
        : this(inlineEngine, tableEngine, floatEngine, new BlockFormattingContext(), diagnosticsSession)
    {
    }

    internal BlockLayoutEngine(
        IInlineLayoutEngine inlineEngine,
        ITableLayoutEngine tableEngine,
        IFloatLayoutEngine floatEngine,
        IBlockFormattingContext blockFormattingContext,
        DiagnosticsSession? diagnosticsSession = null)
    {
        _inlineEngine = inlineEngine ?? throw new ArgumentNullException(nameof(inlineEngine));
        _measurement = new BlockMeasurementService(_inlineEngine);
        _tableEngine = tableEngine ?? throw new ArgumentNullException(nameof(tableEngine));
        _floatEngine = floatEngine ?? throw new ArgumentNullException(nameof(floatEngine));
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _diagnosticsSession = diagnosticsSession;
    }

    public BoxTree Layout(DisplayNode displayRoot, PageBox page)
    {
        if (displayRoot is null)
        {
            throw new ArgumentNullException(nameof(displayRoot));
        }

        var tree = new BoxTree();
        CopyPageTo(tree.Page, page);

        var contentX = page.Margin.Left;
        var contentY = page.Margin.Top;
        var pageSize = page.Size;
        var contentWidth = pageSize.Width - page.Margin.Left - page.Margin.Right;

        var candidates = SelectTopLevelCandidates(displayRoot);

        var currentY = contentY;
        var previousBottomMargin = 0f;

        foreach (var child in candidates)
        {
            switch (child)
            {
                case TableBox table:
                {
                    var marginTop = table.Style.Margin.Safe().Top;
                    var collapsedTop = Math.Max(previousBottomMargin, marginTop);
                    var tableBlock = LayoutTable(table, contentX, currentY, contentWidth, collapsedTop);
                    tree.Blocks.Add(tableBlock);
                    currentY = tableBlock.Y + tableBlock.Height;
                    previousBottomMargin = tableBlock.Margin.Bottom;
                    break;
                }
                case BlockBox box:
                {
                    var marginTop = box.Style.Margin.Safe().Top;
                    var collapsedTop = Math.Max(previousBottomMargin, marginTop);
                    PublishMarginCollapse(previousBottomMargin, marginTop, collapsedTop);
                    var block = LayoutBlock(
                        box,
                        contentX,
                        currentY,
                        contentWidth,
                        contentY,
                        previousBottomMargin,
                        collapsedTop);
                    tree.Blocks.Add(block);
                    currentY = block.Y + block.Height;
                    previousBottomMargin = block.Margin.Bottom;
                    break;
                }
            }
        }

        return tree;
    }

    private static IReadOnlyList<DisplayNode> SelectTopLevelCandidates(DisplayNode displayRoot)
    {
        if (displayRoot is TableBox tableRoot)
        {
            return [tableRoot];
        }

        if (displayRoot is BlockBox rootBlock)
        {
            return IsInlineOnlyBlock(rootBlock) 
                ? [rootBlock] 
                : rootBlock.Children;
        }

        return [displayRoot];
    }

    private static bool IsInlineOnlyBlock(BlockBox block)
    {
        var hasInline = block.Children.Any(c => c is InlineBox);
        var hasBlockOrTable = block.Children.Any(static c => c is BlockBox);
        return hasInline && !hasBlockOrTable;
    }

    private BlockBox LayoutBlock(
        BlockBox node,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop,
        float previousBottomMargin,
        float collapsedTopMargin)
    {
        var measurement = _measurement.Prepare(node, contentWidth);
        var margin = measurement.Margin;
        var padding = measurement.Padding;
        var border = measurement.Border;

        var rawX = contentX + margin.Left;
        var rawY = cursorY + collapsedTopMargin;
        var x = Math.Max(rawX, contentX);
        var y = Math.Max(rawY, parentContentTop);

        // Content width accounts for padding and borders
        var contentWidthForChildren = measurement.ContentWidth;
        var contentXForChildren = x + padding.Left + border.Left;
        var contentYForChildren = y + padding.Top + border.Top;

        if (node.MarkerOffset > 0f)
        {
            contentXForChildren += node.MarkerOffset;
        }

        // use inline engine for height estimation (use content width)
        _floatEngine.PlaceFloats(node, x, y, measurement.ResolvedWidth); 

        var nestedBlocksHeight = LayoutChildBlocks(
            node,
            contentXForChildren,
            contentYForChildren,
            contentWidthForChildren,
            contentYForChildren);
        var canonicalBlockHeight = ResolveCanonicalBlockHeight(node, contentWidthForChildren);
        var sequentialContentHeight = ResolveSequentialContentHeight(node, contentWidthForChildren);
        var contentHeight = _measurement.ResolveContentHeight(
            node,
            contentWidthForChildren,
            _ => Math.Max(nestedBlocksHeight, canonicalBlockHeight),
            sequentialContentHeight);
        var contentSize = new SizePt(measurement.ResolvedWidth, contentHeight).Safe().ClampMin(0f, 0f);

        node.X = x;
        node.Y = y;
        node.Width = contentSize.Width; // Total width (for fragment Rect)
        node.Height = contentSize.Height + padding.Vertical + border.Vertical;
        node.Margin = margin;
        node.Padding = padding;
        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        return node;
    }
    
    private float LayoutChildBlocks(
        BlockBox parent,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop)
    {
        var currentY = cursorY;
        var previousBottomMargin = 0f;

        for (var i = 0; i < parent.Children.Count; i++)
        {
            switch (parent.Children[i])
            {
                case TableBox tableChild:
                {
                    var marginTop = tableChild.Style.Margin.Safe().Top;
                    var collapsedTop = Math.Max(previousBottomMargin, marginTop);
                    PublishMarginCollapse(previousBottomMargin, marginTop, collapsedTop);
                    var laidOutTable = LayoutTable(tableChild, contentX, currentY, contentWidth, collapsedTop);
                    parent.Children[i] = laidOutTable;
                    currentY = laidOutTable.Y + laidOutTable.Height;
                    previousBottomMargin = laidOutTable.Margin.Bottom;
                    break;
                }
                case BlockBox blockChild:
                {
                    var marginTop = blockChild.Style.Margin.Safe().Top;
                    var collapsedTop = Math.Max(previousBottomMargin, marginTop);
                    PublishMarginCollapse(previousBottomMargin, marginTop, collapsedTop);
                    LayoutBlock(
                        blockChild,
                        contentX,
                        currentY,
                        contentWidth,
                        parentContentTop,
                        previousBottomMargin,
                        collapsedTop);
                    currentY = blockChild.Y + blockChild.Height;
                    previousBottomMargin = blockChild.Margin.Bottom;
                    break;
                }
            }
        }

        return Math.Max(0, (currentY + previousBottomMargin) - cursorY);
    }

    private void PublishMarginCollapse(float previousBottomMargin, float nextTopMargin, float collapsedTopMargin)
    {
        _diagnosticsSession?.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Trace,
            Name = "layout/margin-collapse",
            Payload = new MarginCollapsePayload
            {
                PreviousBottomMargin = previousBottomMargin,
                NextTopMargin = nextTopMargin,
                CollapsedTopMargin = collapsedTopMargin
            }
        });
    }

    private TableBox LayoutTable(TableBox node, float contentX, float cursorY, float contentWidth, float collapsedTopMargin)
    {
        var s = node.Style;
        var margin = s.Margin.Safe();

        var x = contentX + margin.Left;
        var y = cursorY + collapsedTopMargin;
        var width = Math.Max(0, contentWidth - margin.Left - margin.Right);
        var result = _tableEngine.Layout(node, width);
        if (!result.IsSupported)
        {
            TableLayoutDiagnostics.EmitUnsupportedTable(
                _diagnosticsSession,
                DisplayNodePathBuilder.Build(node),
                result.UnsupportedStructureKind ?? "unsupported-table-structure",
                result.UnsupportedReason ?? "Unsupported table structure.",
                result.RowCount,
                result.RequestedWidth,
                result.ResolvedWidth,
                groupContexts: BuildTableGroupContexts(node));

            return CreateZeroHeightUnsupportedPlaceholder(node, x, y, result.ResolvedWidth, margin);
        }

        TableLayoutDiagnostics.EmitSupportedTable(
            _diagnosticsSession,
            DisplayNodePathBuilder.Build(node),
            result.Rows.Count,
            result.DerivedColumnCount,
            result.RequestedWidth,
            result.ResolvedWidth,
            BuildTableRowContexts(result),
            BuildTableCellContexts(result),
            BuildTableColumnContexts(result),
            BuildTableGroupContexts(node));

        node.DerivedColumnCount = result.DerivedColumnCount;
        return MaterializeTableBlock(node, result, x, y, margin);
    }

    private static IReadOnlyList<TableRowDiagnosticContext> BuildTableRowContexts(TableLayoutResult result)
    {
        return result.Rows
            .Select(static row => new TableRowDiagnosticContext(
                row.RowIndex,
                row.Cells.Count,
                row.Height))
            .ToList();
    }

    private static IReadOnlyList<TableCellDiagnosticContext> BuildTableCellContexts(TableLayoutResult result)
    {
        return result.Rows
            .SelectMany(static row => row.Cells.Select(cell => new TableCellDiagnosticContext(
                row.RowIndex,
                cell.ColumnIndex,
                cell.IsHeader,
                cell.Width,
                cell.Height)))
            .ToList();
    }

    private static IReadOnlyList<TableColumnDiagnosticContext> BuildTableColumnContexts(TableLayoutResult result)
    {
        return result.ColumnWidths
            .Select(static (width, index) => new TableColumnDiagnosticContext(index, width))
            .ToList();
    }

    private static IReadOnlyList<TableGroupDiagnosticContext> BuildTableGroupContexts(TableBox table)
    {
        var groups = table.Children
            .OfType<TableSectionBox>()
            .Select(static section => new TableGroupDiagnosticContext(
                section.Element?.TagName.ToLowerInvariant() ?? "section",
                section.Children.OfType<TableRowBox>().Count()))
            .ToList();

        if (groups.Count > 0)
        {
            return groups;
        }

        return
        [
            new TableGroupDiagnosticContext(
                "direct",
                table.Children.OfType<TableRowBox>().Count())
        ];
    }

    private TableBox MaterializeTableBlock(
        TableBox sourceTable,
        TableLayoutResult result,
        float x,
        float y,
        Spacing margin)
    {
        var tableBlock = CreateTableBlock(sourceTable, x, y, result.ResolvedWidth, result.Height, margin);

        foreach (var rowResult in result.Rows)
        {
            tableBlock.Children.Add(MapTableRowBlock(rowResult, tableBlock, x, y, result.ResolvedWidth));
        }

        return tableBlock;
    }

    private static TableBox CreateTableBlock(
        TableBox sourceTable,
        float x,
        float y,
        float width,
        float height,
        Spacing margin)
    {
        return new TableBox(DisplayRole.Table)
        {
            Parent = sourceTable.Parent,
            Element = sourceTable.Element,
            Style = sourceTable.Style,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Margin = margin,
            Padding = sourceTable.Padding,
            TextAlign = sourceTable.TextAlign,
            MarkerOffset = sourceTable.MarkerOffset,
            DerivedColumnCount = sourceTable.DerivedColumnCount,
            IsAnonymous = sourceTable.IsAnonymous,
            IsInlineBlockContext = sourceTable.IsInlineBlockContext
        };
    }

    private static TableBox CreateZeroHeightUnsupportedPlaceholder(
        TableBox sourceTable,
        float x,
        float y,
        float width,
        Spacing margin)
    {
        return CreateTableBlock(sourceTable, x, y, width, 0f, margin);
    }

    private TableRowBox MapTableRowBlock(
        TableLayoutRowResult rowResult,
        TableBox tableBlock,
        float tableX,
        float tableY,
        float tableWidth)
    {
        var rowBlock = new TableRowBox(DisplayRole.TableRow)
        {
            Parent = tableBlock,
            Element = rowResult.SourceRow.Element,
            Style = rowResult.SourceRow.Style,
            X = tableX,
            Y = tableY + rowResult.Y,
            Width = tableWidth,
            Height = rowResult.Height,
            Margin = rowResult.SourceRow.Margin,
            Padding = rowResult.SourceRow.Padding,
            RowIndex = rowResult.RowIndex,
            TextAlign = rowResult.SourceRow.TextAlign,
            MarkerOffset = rowResult.SourceRow.MarkerOffset,
            IsAnonymous = rowResult.SourceRow.IsAnonymous,
            IsInlineBlockContext = rowResult.SourceRow.IsInlineBlockContext
        };

        foreach (var placement in rowResult.Cells)
        {
            rowBlock.Children.Add(MapTableCellBlock(placement, rowBlock, tableX, tableY));
        }

        return rowBlock;
    }

    private TableCellBox MapTableCellBlock(
        TableLayoutCellPlacement placement,
        TableRowBox rowBlock,
        float tableX,
        float tableY)
    {
        var sourceCell = placement.SourceCell;
        var cellBlock = new TableCellBox(DisplayRole.TableCell)
        {
            Parent = rowBlock,
            Element = sourceCell.Element,
            Style = sourceCell.Style,
            X = tableX + placement.X,
            Y = tableY + placement.Y,
            Width = placement.Width,
            Height = placement.Height,
            Margin = sourceCell.Style.Margin.Safe(),
            Padding = sourceCell.Style.Padding.Safe(),
            ColumnIndex = placement.ColumnIndex,
            IsHeader = placement.IsHeader,
            TextAlign = sourceCell.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign,
            MarkerOffset = sourceCell.MarkerOffset,
            IsAnonymous = sourceCell.IsAnonymous,
            IsInlineBlockContext = sourceCell.IsInlineBlockContext
        };

        foreach (var child in sourceCell.Children)
        {
            cellBlock.Children.Add(MapTableContentNode(child, cellBlock));
        }

        LayoutMappedBlockChildren(cellBlock);
        return cellBlock;
    }

    private void LayoutMappedBlockChildren(BlockBox block)
    {
        var padding = block.Padding.Safe();
        var border = Spacing.FromBorderEdges(block.Style.Borders).Safe();
        var contentX = block.X + padding.Left + border.Left;
        var contentY = block.Y + padding.Top + border.Top;
        var contentWidth = Math.Max(0f, block.Width - padding.Horizontal - border.Horizontal);

        if (block.MarkerOffset > 0f)
        {
            contentX += block.MarkerOffset;
            contentWidth = Math.Max(0f, contentWidth - block.MarkerOffset);
        }

        LayoutChildBlocks(
            block,
            contentX,
            contentY,
            contentWidth,
            contentY);
    }

    private static void CopyPageTo(PageBox target, PageBox source)
    {
        target.Size = source.Size;
        target.Margin = source.Margin;
    }

    private float ResolveCanonicalBlockHeight(BlockBox node, float contentWidthForChildren)
    {
        if (!node.Children.OfType<BlockBox>().Any())
        {
            return 0f;
        }

        var request = float.IsFinite(contentWidthForChildren)
            ? BlockFormattingRequest.ForTopLevel(node, Math.Max(0f, contentWidthForChildren))
            : BlockFormattingRequest.ForUnboundedWidth(FormattingContextKind.Block, node);
        var result = _blockFormattingContext.Format(request);

        var formattedChildren = result.FormattedBlocks
            .Where(static block => block.Role == DisplayRole.Block)
            .Where(block => !ReferenceEquals(block, node))
            .ToList();

        if (formattedChildren.Count == 0)
        {
            return 0f;
        }

        var minY = float.PositiveInfinity;
        var maxY = float.NegativeInfinity;
        foreach (var block in formattedChildren)
        {
            var margin = block.Style.Margin.Safe();
            var top = block.Y - margin.Top;
            var bottom = block.Y + block.Height + margin.Bottom;
            minY = Math.Min(minY, top);
            maxY = Math.Max(maxY, bottom);
        }

        return Math.Max(0f, maxY - minY);
    }

    private float ResolveSequentialContentHeight(BlockBox node, float contentWidth)
    {
        var currentY = 0f;
        var previousBottomMargin = 0f;
        var pendingInlineFlow = new List<InlineBox>();

        foreach (var child in node.Children)
        {
            if (TryAppendInlineFlowMeasurementNode(child, pendingInlineFlow))
            {
                continue;
            }

            currentY = FlushInlineFlowHeight(node, pendingInlineFlow, contentWidth, currentY, ref previousBottomMargin);

            switch (child)
            {
                case TableBox table:
                {
                    var margin = table.Style.Margin.Safe();
                    var collapsedTop = Math.Max(previousBottomMargin, margin.Top);
                    currentY += collapsedTop + table.Height;
                    previousBottomMargin = margin.Bottom;
                    break;
                }
                case BlockBox block:
                {
                    var margin = block.Style.Margin.Safe();
                    var collapsedTop = Math.Max(previousBottomMargin, margin.Top);
                    currentY += collapsedTop + block.Height;
                    previousBottomMargin = margin.Bottom;
                    break;
                }
            }
        }

        currentY = FlushInlineFlowHeight(node, pendingInlineFlow, contentWidth, currentY, ref previousBottomMargin);
        return Math.Max(0f, currentY + previousBottomMargin);
    }

    private static bool TryAppendInlineFlowMeasurementNode(DisplayNode node, ICollection<InlineBox> pendingInlineFlow)
    {
        switch (node)
        {
            case InlineBox inline:
                pendingInlineFlow.Add(inline);
                return true;
            case InlineBlockBoundaryBox boundary:
                pendingInlineFlow.Add(boundary.SourceInline);
                return true;
            case BlockBox block when IsAnonymousInlineWrapper(block):
                foreach (var inline in block.Children.OfType<InlineBox>())
                {
                    pendingInlineFlow.Add(inline);
                }

                return true;
            default:
                return false;
        }
    }

    private static bool IsAnonymousInlineWrapper(BlockBox block)
    {
        return block.IsAnonymous &&
               block.Children.Count > 0 &&
               block.Children.All(static child => child is InlineBox);
    }

    private float FlushInlineFlowHeight(
        BlockBox source,
        List<InlineBox> pendingInlineFlow,
        float contentWidth,
        float currentY,
        ref float previousBottomMargin)
    {
        if (pendingInlineFlow.Count == 0)
        {
            return currentY;
        }

        currentY += previousBottomMargin;
        previousBottomMargin = 0f;

        var measurementBlock = new BlockBox(DisplayRole.Block)
        {
            Element = source.Element,
            Style = source.Style,
            TextAlign = source.TextAlign,
            MarkerOffset = source.MarkerOffset
        };

        foreach (var inline in pendingInlineFlow)
        {
            measurementBlock.Children.Add(inline);
        }

        var inlineHeight = _inlineEngine.MeasureHeight(measurementBlock, contentWidth);
        pendingInlineFlow.Clear();
        return currentY + inlineHeight;
    }

    private static DisplayNode MapTableContentNode(DisplayNode source, DisplayNode parent)
    {
        DisplayNode mapped = source switch
        {
            TableBox table => new TableBox(table.Role)
            {
                Parent = parent,
                Element = table.Element,
                Style = table.Style,
                X = table.X,
                Y = table.Y,
                Width = table.Width,
                Height = table.Height,
                Margin = table.Margin,
                Padding = table.Padding,
                DerivedColumnCount = table.DerivedColumnCount,
                TextAlign = table.TextAlign,
                MarkerOffset = table.MarkerOffset,
                IsAnonymous = table.IsAnonymous,
                IsInlineBlockContext = table.IsInlineBlockContext
            },
            TableSectionBox section => new TableSectionBox(section.Role)
            {
                Parent = parent,
                Element = section.Element,
                Style = section.Style
            },
            TableRowBox row => new TableRowBox(row.Role)
            {
                Parent = parent,
                Element = row.Element,
                Style = row.Style,
                X = row.X,
                Y = row.Y,
                Width = row.Width,
                Height = row.Height,
                Margin = row.Margin,
                Padding = row.Padding,
                RowIndex = row.RowIndex,
                TextAlign = row.TextAlign,
                MarkerOffset = row.MarkerOffset,
                IsAnonymous = row.IsAnonymous,
                IsInlineBlockContext = row.IsInlineBlockContext
            },
            TableCellBox cell => new TableCellBox(cell.Role)
            {
                Parent = parent,
                Element = cell.Element,
                Style = cell.Style,
                X = cell.X,
                Y = cell.Y,
                Width = cell.Width,
                Height = cell.Height,
                Margin = cell.Margin,
                Padding = cell.Padding,
                ColumnIndex = cell.ColumnIndex,
                IsHeader = cell.IsHeader,
                TextAlign = cell.TextAlign,
                MarkerOffset = cell.MarkerOffset,
                IsAnonymous = cell.IsAnonymous,
                IsInlineBlockContext = cell.IsInlineBlockContext
            },
            InlineBox inline => new InlineBox(inline.Role)
            {
                Parent = parent,
                Element = inline.Element,
                Style = inline.Style,
                TextContent = inline.TextContent,
                Width = inline.Width,
                Height = inline.Height,
                BaselineOffset = inline.BaselineOffset,
                Fragment = inline.Fragment
            },
            InlineBlockBoundaryBox boundary => new InlineBlockBoundaryBox(boundary.SourceInline, boundary.SourceContentBox)
            {
                Parent = parent,
                Element = boundary.Element,
                Style = boundary.Style,
                X = boundary.X,
                Y = boundary.Y,
                Width = boundary.Width,
                Height = boundary.Height,
                Margin = boundary.Margin,
                Padding = boundary.Padding,
                TextAlign = boundary.TextAlign,
                MarkerOffset = boundary.MarkerOffset,
                IsAnonymous = boundary.IsAnonymous,
                IsInlineBlockContext = boundary.IsInlineBlockContext
            },
            BlockBox block => new BlockBox(block.Role)
            {
                Parent = parent,
                Element = block.Element,
                Style = block.Style,
                X = block.X,
                Y = block.Y,
                Width = block.Width,
                Height = block.Height,
                Margin = block.Margin,
                Padding = block.Padding,
                TextAlign = block.TextAlign,
                MarkerOffset = block.MarkerOffset,
                IsAnonymous = block.IsAnonymous,
                IsInlineBlockContext = block.IsInlineBlockContext
            },
            _ => throw new NotSupportedException($"Unsupported display node type: {source.GetType().Name}")
        };

        foreach (var child in source.Children)
        {
            mapped.Children.Add(MapTableContentNode(child, mapped));
        }

        return mapped;
    }

}
