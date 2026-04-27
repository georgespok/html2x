using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Orchestrates current block layout by routing nodes to block, table, inline, and diagnostic formatting context owners.
/// </summary>
public sealed class BlockLayoutEngine
{
    private readonly InlineLayoutEngine _inlineEngine;
    private readonly BlockMeasurementService _measurement;
    private readonly TableLayoutEngine _tableEngine;
    private readonly DiagnosticsSession? _diagnosticsSession;
    private readonly IBlockFormattingContext _blockFormattingContext;
    private readonly IImageLayoutResolver _imageResolver;
    private readonly TablePlacementApplier _tablePlacementApplier;

    public BlockLayoutEngine(
        InlineLayoutEngine inlineEngine,
        TableLayoutEngine tableEngine,
        DiagnosticsSession? diagnosticsSession = null)
        : this(
            inlineEngine,
            tableEngine,
            new BlockFormattingContext(),
            new ImageLayoutResolver(),
            diagnosticsSession)
    {
    }

    internal BlockLayoutEngine(
        InlineLayoutEngine inlineEngine,
        TableLayoutEngine tableEngine,
        IBlockFormattingContext blockFormattingContext,
        IImageLayoutResolver imageResolver,
        DiagnosticsSession? diagnosticsSession = null)
    {
        ArgumentNullException.ThrowIfNull(inlineEngine);
        ArgumentNullException.ThrowIfNull(tableEngine);
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _measurement = new BlockMeasurementService(_blockFormattingContext);
        _inlineEngine = inlineEngine;
        _tableEngine = tableEngine;
        _imageResolver = imageResolver ?? throw new ArgumentNullException(nameof(imageResolver));
        _tablePlacementApplier = new TablePlacementApplier();
        _diagnosticsSession = diagnosticsSession;
    }

    public BoxTree Layout(BoxNode boxRoot, PageBox page)
    {
        if (boxRoot is null)
        {
            throw new ArgumentNullException(nameof(boxRoot));
        }

        var tree = new BoxTree();
        CopyPageTo(tree.Page, page);

        var contentArea = PageContentArea.From(page.Size, page.Margin);
        var contentX = contentArea.X;
        var contentY = contentArea.Y;
        var contentWidth = contentArea.Width;

        var candidates = SelectTopLevelCandidates(boxRoot);

        var currentY = contentY;
        var previousBottomMargin = 0f;

        foreach (var child in candidates)
        {
            if (child is not BlockBox block)
            {
                continue;
            }

            var marginTop = block.Style.Margin.Safe().Top;
            var collapsedTop = VerticalFlowPolicy.CollapseTopMargin(
                _blockFormattingContext,
                previousBottomMargin,
                marginTop,
                FormattingContextKind.Block,
                nameof(BlockLayoutEngine),
                _diagnosticsSession);
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
            currentY = VerticalFlowPolicy.AdvanceCursorPast(laidOutBlock.Y, laidOutBlock.Height);
            previousBottomMargin = laidOutBlock.Margin.Bottom;
        }

        return tree;
    }

    private static IReadOnlyList<BoxNode> SelectTopLevelCandidates(BoxNode boxRoot)
    {
        if (boxRoot is TableBox tableRoot)
        {
            return [tableRoot];
        }

        if (boxRoot is BlockBox rootBlock)
        {
            return IsInlineOnlyBlock(rootBlock) 
                ? [rootBlock] 
                : rootBlock.Children;
        }

        return [boxRoot];
    }

    private static bool IsInlineOnlyBlock(BlockBox block)
    {
        var hasInline = block.Children.Any(c => c is InlineBox);
        var hasBlockOrTable = block.Children.Any(static c => c is BlockBox);
        return hasInline && !hasBlockOrTable;
    }

    private BlockBox LayoutBlock(BlockBox node, BlockLayoutRequest request)
    {
        return node switch
        {
            TableBox table => LayoutTableBlock(table, request),
            ImageBox image => LayoutImageBlock(image, request),
            RuleBox rule => LayoutRuleBlock(rule, request),
            _ => LayoutStandardBlock(node, request)
        };
    }

    internal BlockBox LayoutStandardBlock(BlockBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var margin = measurement.Margin;
        var padding = measurement.Padding;
        var border = measurement.Border;

        var origin = BlockPlacementService.ResolveOrigin(request, margin);
        var x = origin.X;
        var y = origin.Y;

        var contentArea = ResolveInitialContentFlowArea(x, y, measurement, node.MarkerOffset);
        var contentWidthForChildren = contentArea.Width;
        var contentXForChildren = contentArea.X;
        var contentYForChildren = contentArea.Y;

        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;

        var flowLayout = LayoutFlowContent(
            node,
            contentXForChildren,
            contentYForChildren,
            contentWidthForChildren,
            contentYForChildren);
        var contentHeight = _measurement.ResolveContentHeight(
            node,
            flowLayout.ContentHeight);
        var borderBoxWidth = BoxGeometryFactory.RequireNonNegativeFinite(measurement.BorderBoxWidth);
        var contentBoxHeight = BoxGeometryFactory.RequireNonNegativeFinite(contentHeight);
        ApplyBlockLayoutState(
            node,
            measurement,
            CreateGeometryFromContentHeight(
                x,
                y,
                borderBoxWidth,
                contentBoxHeight,
                padding,
                border,
                node.MarkerOffset));

        return node;
    }

    internal BlockBox LayoutImageBlock(ImageBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var image = _imageResolver.Resolve(node, measurement.ContentFlowWidth);
        var padding = measurement.Padding;
        var border = measurement.Border;
        var origin = BlockPlacementService.ResolveOrigin(request, measurement.Margin);
        var x = origin.X;
        var y = origin.Y;

        node.Src = image.Src;
        node.AuthoredSizePx = image.AuthoredSizePx;
        node.IntrinsicSizePx = image.IntrinsicSizePx;
        node.IsMissing = image.IsMissing;
        node.IsOversize = image.IsOversize;
        ApplyBlockLayoutState(
            node,
            measurement,
            BoxGeometryFactory.FromBorderBox(
                x,
                y,
                image.TotalWidth,
                image.TotalHeight,
                padding,
                border,
                markerOffset: node.MarkerOffset));

        return node;
    }

    internal BlockBox LayoutRuleBlock(RuleBox node, BlockLayoutRequest request)
    {
        var measurement = _measurement.Prepare(node, request.ContentWidth);
        var origin = BlockPlacementService.ResolveOrigin(request, measurement.Margin);
        var x = origin.X;
        var y = origin.Y;

        ApplyBlockLayoutState(
            node,
            measurement,
            CreateGeometryFromContentHeight(
                x,
                y,
                measurement.BorderBoxWidth,
                0f,
                measurement.Padding,
                measurement.Border,
                node.MarkerOffset));

        return node;
    }
    
    private float LayoutChildBlocks(
        BlockBox parent,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop)
    {
        return LayoutFlowContent(parent, contentX, cursorY, contentWidth, parentContentTop).ContentHeight;
    }

    private BlockFlowLayoutResult LayoutFlowContent(
        BlockBox parent,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop)
    {
        var currentY = cursorY;
        var previousBottomMargin = 0f;
        var pendingInlineFlow = new InlineFlowBuffer();
        var inlineSegments = new List<InlineFlowSegmentLayout>();
        var maxLineWidth = 0f;
        var includeSyntheticListMarker = true;

        for (var i = 0; i < parent.Children.Count; i++)
        {
            if (pendingInlineFlow.TryQueue(parent.Children[i]))
            {
                continue;
            }

            FlushInlineFlow(
                parent,
                pendingInlineFlow,
                inlineSegments,
                contentX,
                contentWidth,
                ref currentY,
                ref previousBottomMargin,
                ref maxLineWidth,
                ref includeSyntheticListMarker);

            if (parent.Children[i] is not BlockBox childBlock)
            {
                continue;
            }

            var marginTop = childBlock.Style.Margin.Safe().Top;
            var collapsedTop = VerticalFlowPolicy.CollapseTopMargin(
                _blockFormattingContext,
                previousBottomMargin,
                marginTop,
                ResolveFormattingContext(parent),
                nameof(BlockLayoutEngine),
                _diagnosticsSession);
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
            currentY = VerticalFlowPolicy.AdvanceCursorPast(laidOutChild.Y, laidOutChild.Height);
            previousBottomMargin = laidOutChild.Margin.Bottom;
        }

        FlushInlineFlow(
            parent,
            pendingInlineFlow,
            inlineSegments,
            contentX,
            contentWidth,
            ref currentY,
            ref previousBottomMargin,
            ref maxLineWidth,
            ref includeSyntheticListMarker);

        var contentHeight = VerticalFlowPolicy.ResolveStackHeight(currentY, previousBottomMargin, cursorY);
        parent.InlineLayout = new InlineLayoutResult(inlineSegments, contentHeight, maxLineWidth);
        return new BlockFlowLayoutResult(contentHeight);
    }

    private void FlushInlineFlow(
        BlockBox blockContext,
        InlineFlowBuffer pendingInlineFlow,
        List<InlineFlowSegmentLayout> inlineSegments,
        float contentX,
        float contentWidth,
        ref float currentY,
        ref float previousBottomMargin,
        ref float maxLineWidth,
        ref bool includeSyntheticListMarker)
    {
        if (pendingInlineFlow.Count == 0)
        {
            return;
        }

        var contentTop = currentY + previousBottomMargin;
        var segmentBlock = CreateInlineSegmentBlock(blockContext, pendingInlineFlow.Nodes);
        var inlineLayout = _inlineEngine.Layout(
            segmentBlock,
            new InlineLayoutRequest(
                contentX,
                contentTop,
                contentWidth,
                includeSyntheticListMarker));

        pendingInlineFlow.Clear();
        includeSyntheticListMarker = false;
        previousBottomMargin = 0f;

        if (inlineLayout.Segments.Count > 0)
        {
            inlineSegments.AddRange(inlineLayout.Segments);
        }

        currentY = contentTop + inlineLayout.TotalHeight;
        maxLineWidth = Math.Max(maxLineWidth, inlineLayout.MaxLineWidth);
    }

    private static BlockBox CreateInlineSegmentBlock(BlockBox blockContext, IReadOnlyList<BoxNode> inlineFlow)
    {
        var segmentBlock = new BlockBox(blockContext.Role)
        {
            Element = blockContext.Element,
            Style = blockContext.Style,
            Parent = blockContext,
            TextAlign = blockContext.TextAlign,
            IsInlineBlockContext = blockContext.IsInlineBlockContext
        };

        segmentBlock.MarkerOffset = blockContext.MarkerOffset;

        foreach (var child in inlineFlow)
        {
            segmentBlock.Children.Add(child);
        }

        return segmentBlock;
    }

    private static ContentFlowArea ResolveInitialContentFlowArea(
        float x,
        float y,
        BlockMeasurementBasis measurement,
        float markerOffset)
    {
        return BoxGeometryFactory.ResolveContentFlowArea(
            x,
            y,
            measurement.BorderBoxWidth,
            0f,
            measurement.Padding,
            measurement.Border,
            markerOffset);
    }

    private static UsedGeometry CreateGeometryFromContentHeight(
        float x,
        float y,
        float borderBoxWidth,
        float contentHeight,
        Spacing padding,
        Spacing border,
        float markerOffset)
    {
        return BoxGeometryFactory.FromBorderBoxWithContentHeight(
            x,
            y,
            borderBoxWidth,
            contentHeight,
            padding,
            border,
            markerOffset: markerOffset);
    }

    private static void ApplyBlockLayoutState(
        BlockBox node,
        BlockMeasurementBasis measurement,
        UsedGeometry geometry)
    {
        node.Margin = measurement.Margin;
        node.Padding = measurement.Padding;
        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        node.ApplyLayoutGeometry(geometry);
    }

    internal TableBox LayoutTableBlock(TableBox node, BlockLayoutRequest request)
    {
        var s = node.Style;
        var margin = s.Margin.Safe();

        var origin = BlockPlacementService.ResolveOrigin(request, margin);
        var x = origin.X;
        var y = origin.Y;
        var result = _tableEngine.Layout(node, request.ContentWidth);
        if (!result.IsSupported)
        {
            TableLayoutDiagnostics.EmitUnsupportedTable(
                _diagnosticsSession,
                BoxNodePathBuilder.Build(node),
                result.UnsupportedStructureKind ?? "unsupported-table-structure",
                result.UnsupportedReason ?? "Unsupported table structure.",
                result.RowCount,
                result.RequestedWidth,
                result.ResolvedWidth,
                groupContexts: BuildTableGroupContexts(node));

            return TablePlacementApplier.ApplyUnsupportedPlaceholder(node, x, y, result.ResolvedWidth, margin);
        }

        TableLayoutDiagnostics.EmitSupportedTable(
            _diagnosticsSession,
            BoxNodePathBuilder.Build(node),
            result.Rows.Count,
            result.DerivedColumnCount,
            result.RequestedWidth,
            result.ResolvedWidth,
            BuildTableRowContexts(result),
            BuildTableCellContexts(result),
            BuildTableColumnContexts(result),
            BuildTableGroupContexts(node));

        return _tablePlacementApplier.ApplySupported(node, result, x, y, margin, LayoutChildBlocks);
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

    private static void CopyPageTo(PageBox target, PageBox source)
    {
        target.Size = source.Size;
        target.Margin = source.Margin;
    }

    private static FormattingContextKind ResolveFormattingContext(BlockBox block) =>
        block.IsInlineBlockContext ? FormattingContextKind.InlineBlock : FormattingContextKind.Block;

    private readonly record struct BlockFlowLayoutResult(float ContentHeight);

}
