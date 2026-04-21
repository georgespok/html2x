using System.Drawing;
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
    private readonly IImageLayoutResolver _imageResolver;
    private readonly BlockLayoutStrategyRegistry _layoutStrategies;

    public BlockLayoutEngine(
        IInlineLayoutEngine inlineEngine,
        ITableLayoutEngine tableEngine,
        IFloatLayoutEngine floatEngine,
        DiagnosticsSession? diagnosticsSession = null)
        : this(
            inlineEngine,
            tableEngine,
            floatEngine,
            new BlockFormattingContext(),
            new ImageLayoutResolver(),
            BlockLayoutStrategyRegistry.CreateDefault(),
            diagnosticsSession)
    {
    }

    internal BlockLayoutEngine(
        IInlineLayoutEngine inlineEngine,
        ITableLayoutEngine tableEngine,
        IFloatLayoutEngine floatEngine,
        IBlockFormattingContext blockFormattingContext,
        IImageLayoutResolver imageResolver,
        BlockLayoutStrategyRegistry layoutStrategies,
        DiagnosticsSession? diagnosticsSession = null)
    {
        _inlineEngine = inlineEngine ?? throw new ArgumentNullException(nameof(inlineEngine));
        _measurement = new BlockMeasurementService();
        _tableEngine = tableEngine ?? throw new ArgumentNullException(nameof(tableEngine));
        _floatEngine = floatEngine ?? throw new ArgumentNullException(nameof(floatEngine));
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _imageResolver = imageResolver ?? throw new ArgumentNullException(nameof(imageResolver));
        _layoutStrategies = layoutStrategies ?? throw new ArgumentNullException(nameof(layoutStrategies));
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
            if (child is not BlockBox block)
            {
                continue;
            }

            var marginTop = block.Style.Margin.Safe().Top;
            var collapsedTop = Math.Max(previousBottomMargin, marginTop);
            PublishMarginCollapse(previousBottomMargin, marginTop, collapsedTop);
            var laidOutBlock = LayoutBlock(
                block,
                new BlockLayoutRequest(
                    contentX,
                    currentY,
                    contentWidth,
                    contentY,
                    previousBottomMargin,
                    collapsedTop));
            tree.Blocks.Add(laidOutBlock);
            currentY = laidOutBlock.Y + laidOutBlock.Height;
            previousBottomMargin = laidOutBlock.Margin.Bottom;
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

    private BlockBox LayoutBlock(BlockBox node, BlockLayoutRequest request)
    {
        return _layoutStrategies.Layout(this, node, request);
    }

    internal BlockBox LayoutStandardBlock(BlockBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var margin = measurement.Margin;
        var padding = measurement.Padding;
        var border = measurement.Border;

        var rawX = request.ContentX + margin.Left;
        var rawY = request.CursorY + request.CollapsedTopMargin;
        var x = Math.Max(rawX, request.ContentX);
        var y = Math.Max(rawY, request.ParentContentTop);

        // Content width accounts for padding and borders
        var contentWidthForChildren = measurement.ContentWidth;
        var contentXForChildren = x + padding.Left + border.Left;
        var contentYForChildren = y + padding.Top + border.Top;
        var inlineContentX = x + padding.Left + border.Left;

        if (node.MarkerOffset > 0f)
        {
            contentXForChildren += node.MarkerOffset;
        }

        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        // use inline engine for height estimation (use content width)
        _floatEngine.PlaceFloats(node, x, y, measurement.ResolvedWidth); 

        var nestedBlocksHeight = LayoutChildBlocks(
            node,
            contentXForChildren,
            contentYForChildren,
            contentWidthForChildren,
            contentYForChildren);
        var canonicalBlockHeight = ResolveCanonicalBlockHeight(node, contentWidthForChildren);
        var inlineLayout = _inlineEngine.Layout(
            node,
            new InlineLayoutRequest(
                inlineContentX,
                contentYForChildren,
                contentWidthForChildren));
        var sequentialContentHeight = inlineLayout.TotalHeight;
        var resolvedContentHeight = Math.Max(
            sequentialContentHeight,
            Math.Max(nestedBlocksHeight, canonicalBlockHeight));
        var contentHeight = _measurement.ResolveContentHeight(
            node,
            resolvedContentHeight,
            sequentialContentHeight);
        var contentSize = new SizePt(measurement.ResolvedWidth, contentHeight).Safe().ClampMin(0f, 0f);
        var usedGeometry = CreateUsedGeometry(
            x,
            y,
            contentSize.Width,
            contentSize.Height + padding.Vertical + border.Vertical,
            padding,
            border,
            markerOffset: node.MarkerOffset);

        node.Margin = margin;
        node.Padding = padding;
        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        node.UsedGeometry = usedGeometry;

        return node;
    }

    internal BlockBox LayoutImageBlock(ImageBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var image = _imageResolver.Resolve(node, measurement.ContentWidth);
        var padding = measurement.Padding;
        var border = measurement.Border;
        var rawX = request.ContentX + measurement.Margin.Left;
        var rawY = request.CursorY + request.CollapsedTopMargin;
        var x = Math.Max(rawX, request.ContentX);
        var y = Math.Max(rawY, request.ParentContentTop);

        node.Src = image.Src;
        node.AuthoredSizePx = image.AuthoredSizePx;
        node.IntrinsicSizePx = image.IntrinsicSizePx;
        node.IsMissing = image.IsMissing;
        node.IsOversize = image.IsOversize;
        node.Margin = measurement.Margin;
        node.Padding = padding;
        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        node.UsedGeometry = CreateUsedGeometry(
            x,
            y,
            image.TotalWidth,
            image.TotalHeight,
            padding,
            border,
            markerOffset: node.MarkerOffset);

        return node;
    }

    internal BlockBox LayoutRuleBlock(RuleBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var rawX = request.ContentX + measurement.Margin.Left;
        var rawY = request.CursorY + request.CollapsedTopMargin;
        var x = Math.Max(rawX, request.ContentX);
        var y = Math.Max(rawY, request.ParentContentTop);

        node.Margin = measurement.Margin;
        node.Padding = measurement.Padding;
        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        node.UsedGeometry = CreateUsedGeometry(
            x,
            y,
            measurement.ResolvedWidth,
            measurement.Padding.Vertical + measurement.Border.Vertical,
            measurement.Padding,
            measurement.Border,
            markerOffset: node.MarkerOffset);

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
            if (parent.Children[i] is not BlockBox childBlock)
            {
                continue;
            }

            var marginTop = childBlock.Style.Margin.Safe().Top;
            var collapsedTop = Math.Max(previousBottomMargin, marginTop);
            PublishMarginCollapse(previousBottomMargin, marginTop, collapsedTop);
            var laidOutChild = LayoutBlock(
                childBlock,
                new BlockLayoutRequest(
                    contentX,
                    currentY,
                    contentWidth,
                    parentContentTop,
                    previousBottomMargin,
                    collapsedTop));
            parent.Children[i] = laidOutChild;
            currentY = laidOutChild.Y + laidOutChild.Height;
            previousBottomMargin = laidOutChild.Margin.Bottom;
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

    internal TableBox LayoutTableBlock(TableBox node, BlockLayoutRequest request)
    {
        var s = node.Style;
        var margin = s.Margin.Safe();

        var x = request.ContentX + margin.Left;
        var y = request.CursorY + request.CollapsedTopMargin;
        var width = Math.Max(0, request.ContentWidth - margin.Left - margin.Right);
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

        return ApplyTableLayout(node, result, x, y, margin);
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

    private TableBox ApplyTableLayout(
        TableBox table,
        TableLayoutResult result,
        float x,
        float y,
        Spacing margin)
    {
        table.Margin = margin;
        table.Padding = table.Style.Padding.Safe();
        table.TextAlign = table.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        table.DerivedColumnCount = result.DerivedColumnCount;
        table.UsedGeometry = CreateUsedGeometry(
            x,
            y,
            result.ResolvedWidth,
            result.Height,
            table.Style.Padding.Safe(),
            Spacing.FromBorderEdges(table.Style.Borders).Safe(),
            markerOffset: table.MarkerOffset);

        foreach (var rowResult in result.Rows)
        {
            ApplyTableRowLayout(rowResult, x, y, result.ResolvedWidth);
        }

        return table;
    }

    private static TableBox CreateZeroHeightUnsupportedPlaceholder(
        TableBox sourceTable,
        float x,
        float y,
        float width,
        Spacing margin)
    {
        sourceTable.Margin = margin;
        sourceTable.Padding = sourceTable.Style.Padding.Safe();
        sourceTable.TextAlign = sourceTable.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        sourceTable.DerivedColumnCount = 0;
        sourceTable.UsedGeometry = CreateUsedGeometry(
            x,
            y,
            width,
            0f,
            sourceTable.Style.Padding.Safe(),
            Spacing.FromBorderEdges(sourceTable.Style.Borders).Safe(),
            markerOffset: sourceTable.MarkerOffset);
        sourceTable.Children.Clear();
        return sourceTable;
    }

    private void ApplyTableRowLayout(
        TableLayoutRowResult rowResult,
        float tableX,
        float tableY,
        float tableWidth)
    {
        var rowBlock = rowResult.SourceRow;
        rowBlock.Margin = rowBlock.Style.Margin.Safe();
        rowBlock.Padding = rowBlock.Style.Padding.Safe();
        rowBlock.RowIndex = rowResult.RowIndex;
        rowBlock.TextAlign = rowBlock.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        rowBlock.UsedGeometry = rowResult.UsedGeometry?.Translate(tableX, tableY) ?? CreateUsedGeometry(
            tableX,
            tableY + rowResult.Y,
            tableWidth,
            rowResult.Height,
            rowBlock.Style.Padding.Safe(),
            Spacing.FromBorderEdges(rowBlock.Style.Borders).Safe(),
            markerOffset: rowBlock.MarkerOffset);

        foreach (var placement in rowResult.Cells)
        {
            ApplyTableCellLayout(placement, tableX, tableY);
        }
    }

    private void ApplyTableCellLayout(
        TableLayoutCellPlacement placement,
        float tableX,
        float tableY)
    {
        var cellBlock = placement.SourceCell;
        cellBlock.Margin = cellBlock.Style.Margin.Safe();
        cellBlock.Padding = cellBlock.Style.Padding.Safe();
        cellBlock.ColumnIndex = placement.ColumnIndex;
        cellBlock.IsHeader = placement.IsHeader;
        cellBlock.TextAlign = cellBlock.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        cellBlock.UsedGeometry = placement.UsedGeometry?.Translate(tableX, tableY) ?? CreateUsedGeometry(
            tableX + placement.X,
            tableY + placement.Y,
            placement.Width,
            placement.Height,
            cellBlock.Style.Padding.Safe(),
            Spacing.FromBorderEdges(cellBlock.Style.Borders).Safe(),
            markerOffset: cellBlock.MarkerOffset);

        LayoutTableCellContent(cellBlock);
    }

    private void LayoutTableCellContent(TableCellBox cell)
    {
        var padding = cell.Padding.Safe();
        var border = Spacing.FromBorderEdges(cell.Style.Borders).Safe();
        var geometry = cell.UsedGeometry;
        var inlineContentX = cell.X + padding.Left + border.Left;
        var contentX = geometry?.ContentBoxRect.X ?? cell.X + padding.Left + border.Left;
        var contentY = geometry?.ContentBoxRect.Y ?? cell.Y + padding.Top + border.Top;
        var contentWidth = geometry?.ContentBoxRect.Width ?? Math.Max(0f, cell.Width - padding.Horizontal - border.Horizontal);

        if (cell.MarkerOffset > 0f)
        {
            contentX += cell.MarkerOffset;
            contentWidth = Math.Max(0f, contentWidth - cell.MarkerOffset);
        }

        LayoutChildBlocks(
            cell,
            contentX,
            contentY,
            contentWidth,
            contentY);
        cell.InlineLayout = _inlineEngine.Layout(
            cell,
            new InlineLayoutRequest(
                inlineContentX,
                contentY,
                contentWidth));
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
