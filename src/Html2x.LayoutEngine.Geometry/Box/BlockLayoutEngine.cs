using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Box.Publishing;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Orchestrates block layout by mutating internal boxes while publishing immutable layout facts.
/// </summary>
/// <remarks>
/// The mutable box path remains an internal layout implementation detail. Production fragment projection
/// consumes <see cref="PublishedLayoutTree"/> so rendering does not read box internals.
/// </remarks>
internal sealed class BlockLayoutEngine
{
    private readonly InlineLayoutEngine _inlineEngine;
    private readonly BlockMeasurementService _measurement;
    private readonly TableLayoutEngine _tableEngine;
    private readonly IDiagnosticsSink? _diagnosticsSink;
    private readonly IBlockFormattingContext _blockFormattingContext;
    private readonly IImageLayoutResolver _imageResolver;
    private readonly TablePlacementApplier _tablePlacementApplier;
    private readonly Dictionary<BoxNode, int> _publishedSourceOrders = [];
    private readonly Dictionary<BlockBox, PublishedBlock> _publishedBlocks = [];
    private int _nextPublishedSourceOrder;

    public BlockLayoutEngine(
        InlineLayoutEngine inlineEngine,
        TableLayoutEngine tableEngine,
        IDiagnosticsSink? diagnosticsSink = null)
        : this(
            inlineEngine,
            tableEngine,
            new BlockFormattingContext(),
            new ImageLayoutResolver(),
            diagnosticsSink)
    {
    }

    internal BlockLayoutEngine(
        InlineLayoutEngine inlineEngine,
        TableLayoutEngine tableEngine,
        IBlockFormattingContext blockFormattingContext,
        IImageLayoutResolver imageResolver,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(inlineEngine);
        ArgumentNullException.ThrowIfNull(tableEngine);
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _measurement = new BlockMeasurementService(_blockFormattingContext);
        _inlineEngine = inlineEngine;
        _tableEngine = tableEngine;
        _imageResolver = imageResolver ?? throw new ArgumentNullException(nameof(imageResolver));
        _tablePlacementApplier = new TablePlacementApplier();
        _diagnosticsSink = diagnosticsSink;
    }

    public BoxTree Layout(BoxNode boxRoot, PageBox page)
    {
        if (boxRoot is null)
        {
            throw new ArgumentNullException(nameof(boxRoot));
        }

        ArgumentNullException.ThrowIfNull(page);

        // Legacy compatibility path for callers that still request mutable boxes.
        // Production fragment projection consumes PublishedLayoutTree.
        ResetPublishedLayoutState();

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
                _diagnosticsSink);
            var laidOutBlock = LayoutBlockForCompatibility(
                block,
                new BlockLayoutRequest(
                    contentX,
                    currentY,
                    contentWidth,
                    contentY,
                    previousBottomMargin,
                    collapsedTop));
            tree.Blocks.Add(laidOutBlock);
            currentY = AdvanceCursorPastUsedGeometry(laidOutBlock);
            previousBottomMargin = laidOutBlock.Margin.Bottom;
        }

        return tree;
    }

    internal PublishedLayoutTree LayoutPublished(BoxNode boxRoot, PageBox page)
    {
        ArgumentNullException.ThrowIfNull(boxRoot);
        ArgumentNullException.ThrowIfNull(page);

        // Phase 3 migration path: layout publishes immutable blocks directly while still applying box state for compatibility.
        ResetPublishedLayoutState();

        var pageFacts = new PublishedPage(page.Size, page.Margin);
        var blocks = LayoutTopLevelBlocksAsPublished(boxRoot, page);

        return new PublishedLayoutTree(pageFacts, blocks);
    }

    private IReadOnlyList<PublishedBlock> LayoutTopLevelBlocksAsPublished(BoxNode boxRoot, PageBox page)
    {
        var contentArea = PageContentArea.From(page.Size, page.Margin);
        var candidates = SelectTopLevelCandidates(boxRoot);
        var published = new List<PublishedBlock>();
        var currentY = contentArea.Y;
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
                _diagnosticsSink);
            var publishedBlock = LayoutBlock(
                block,
                new BlockLayoutRequest(
                    contentArea.X,
                    currentY,
                    contentArea.Width,
                    contentArea.Y,
                    previousBottomMargin,
                    collapsedTop));

            published.Add(publishedBlock);
            currentY = AdvanceCursorPastUsedGeometry(block);
            previousBottomMargin = block.Margin.Bottom;
        }

        return published;
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

    private BlockBox LayoutBlockForCompatibility(BlockBox node, BlockLayoutRequest request)
    {
        return node switch
        {
            TableBox table => LayoutTableBlockForCompatibility(table, request),
            ImageBox image => LayoutImageBlockForCompatibility(image, request),
            RuleBox rule => LayoutRuleBlockForCompatibility(rule, request),
            _ => LayoutStandardBlockForCompatibility(node, request)
        };
    }

    private PublishedBlock LayoutBlock(BlockBox node, BlockLayoutRequest request)
    {
        return node switch
        {
            TableBox table => LayoutTableBlock(table, request),
            ImageBox image => LayoutImageBlock(image, request),
            RuleBox rule => LayoutRuleBlock(rule, request),
            _ => LayoutStandardBlock(node, request)
        };
    }

    private BlockBox LayoutStandardBlockForCompatibility(BlockBox node, BlockLayoutRequest request)
    {
        _ = ApplyStandardBlockLayout(node, request, publishOutput: false);
        return node;
    }

    internal PublishedBlock LayoutStandardBlock(BlockBox node, BlockLayoutRequest request)
    {
        var flowLayout = ApplyStandardBlockLayout(node, request, publishOutput: true);
        var geometry = node.UsedGeometry ?? throw new InvalidOperationException(
            $"Standard block layout requires UsedGeometry for '{BoxNodePathBuilder.Build(node)}'.");
        var identity = CreatePublishedIdentity(node);

        return CachePublishedBlock(
            node,
            PublishedBlockFactory.CreateBlock(
                node,
                identity,
                geometry,
                flowLayout.PublishedInlineLayout,
                flowLayout.PublishedChildren,
                flowLayout.PublishedFlow));
    }

    private BlockFlowLayoutResult ApplyStandardBlockLayout(
        BlockBox node,
        BlockLayoutRequest request,
        bool publishOutput)
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
            contentYForChildren,
            publishOutput);
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

        return flowLayout;
    }

    private BlockBox LayoutImageBlockForCompatibility(ImageBox node, BlockLayoutRequest request)
    {
        ApplyImageBlockLayout(node, request);
        return node;
    }

    internal PublishedBlock LayoutImageBlock(ImageBox node, BlockLayoutRequest request)
    {
        ApplyImageBlockLayout(node, request);
        return PublishResolvedBlock(node);
    }

    private void ApplyImageBlockLayout(ImageBox node, BlockLayoutRequest request)
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
    }

    private BlockBox LayoutRuleBlockForCompatibility(RuleBox node, BlockLayoutRequest request)
    {
        ApplyRuleBlockLayout(node, request);
        return node;
    }

    internal PublishedBlock LayoutRuleBlock(RuleBox node, BlockLayoutRequest request)
    {
        ApplyRuleBlockLayout(node, request);
        return PublishResolvedBlock(node);
    }

    private void ApplyRuleBlockLayout(RuleBox node, BlockLayoutRequest request)
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
    }
    
    private float LayoutChildBlocks(
        BlockBox parent,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop)
    {
        var flowLayout = LayoutFlowContent(parent, contentX, cursorY, contentWidth, parentContentTop, publishOutput: true);
        var geometry = parent.UsedGeometry ?? throw new InvalidOperationException(
            $"Published layout requires UsedGeometry for '{BoxNodePathBuilder.Build(parent)}'.");

        CachePublishedBlock(
            parent,
            PublishedBlockFactory.CreateBlock(
                parent,
                CreatePublishedIdentity(parent),
                geometry,
                flowLayout.PublishedInlineLayout,
                flowLayout.PublishedChildren,
                flowLayout.PublishedFlow));

        return flowLayout.ContentHeight;
    }

    private float LayoutChildBlocksForCompatibility(
        BlockBox parent,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop)
    {
        return LayoutFlowContent(parent, contentX, cursorY, contentWidth, parentContentTop, publishOutput: false)
            .ContentHeight;
    }

    private BlockFlowLayoutResult LayoutFlowContent(
        BlockBox parent,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop,
        bool publishOutput)
    {
        var currentY = cursorY;
        var previousBottomMargin = 0f;
        var pendingInlineFlow = new InlineFlowBuffer();
        var inlineSegments = new List<InlineFlowSegmentLayout>();
        var publishedInlineSegments = new List<PublishedInlineFlowSegment>();
        var publishedChildren = new List<PublishedBlock>();
        var publishedFlow = new List<PublishedBlockFlowItem>();
        var maxLineWidth = 0f;
        var includeSyntheticListMarker = true;
        var nextFlowOrder = 0;

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
                ref includeSyntheticListMarker,
                publishOutput,
                publishedInlineSegments,
                publishedFlow,
                ref nextFlowOrder);

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
                _diagnosticsSink);
            var childRequest = new BlockLayoutRequest(
                contentX,
                currentY,
                contentWidth,
                parentContentTop,
                previousBottomMargin,
                collapsedTop);
            if (publishOutput)
            {
                var publishedChild = LayoutBlock(childBlock, childRequest);
                publishedChildren.Add(publishedChild);
                publishedFlow.Add(new PublishedChildBlockItem(nextFlowOrder++, publishedChild));
            }
            else
            {
                _ = LayoutBlockForCompatibility(childBlock, childRequest);
            }

            parent.Children[i] = childBlock;
            currentY = AdvanceCursorPastUsedGeometry(childBlock);
            previousBottomMargin = childBlock.Margin.Bottom;
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
            ref includeSyntheticListMarker,
            publishOutput,
            publishedInlineSegments,
            publishedFlow,
            ref nextFlowOrder);

        var contentHeight = VerticalFlowPolicy.ResolveStackHeight(currentY, previousBottomMargin, cursorY);
        parent.InlineLayout = new InlineLayoutResult(inlineSegments, contentHeight, maxLineWidth);
        var publishedInlineLayout = publishOutput
            ? new PublishedInlineLayout(publishedInlineSegments, contentHeight, maxLineWidth)
            : null;

        return new BlockFlowLayoutResult(
            contentHeight,
            publishedChildren,
            publishedInlineLayout,
            publishedFlow);
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
        ref bool includeSyntheticListMarker,
        bool publishOutput,
        ICollection<PublishedInlineFlowSegment> publishedInlineSegments,
        ICollection<PublishedBlockFlowItem> publishedFlow,
        ref int nextFlowOrder)
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
            if (publishOutput)
            {
                foreach (var segment in inlineLayout.Segments)
                {
                    var publishedSegment = CreatePublishedInlineSegment(segment);
                    publishedInlineSegments.Add(publishedSegment);
                    publishedFlow.Add(new PublishedInlineFlowSegmentItem(nextFlowOrder++, publishedSegment));
                }
            }
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
            IsInlineBlockContext = blockContext.IsInlineBlockContext,
            SourceIdentity = ResolveInlineSegmentSourceIdentity(blockContext, inlineFlow)
        };

        segmentBlock.MarkerOffset = blockContext.MarkerOffset;

        foreach (var child in inlineFlow)
        {
            segmentBlock.Children.Add(child);
        }

        return segmentBlock;
    }

    private static GeometrySourceIdentity ResolveInlineSegmentSourceIdentity(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineFlow)
    {
        foreach (var child in inlineFlow)
        {
            if (child.SourceIdentity.IsSpecified)
            {
                return child.SourceIdentity.AsGenerated(GeometryGeneratedSourceKind.InlineSegment);
            }
        }

        return blockContext.SourceIdentity.AsGenerated(GeometryGeneratedSourceKind.InlineSegment);
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

    private TableBox LayoutTableBlockForCompatibility(TableBox node, BlockLayoutRequest request)
    {
        ApplyTableBlockLayout(node, request, LayoutChildBlocksForCompatibility);
        return node;
    }

    internal PublishedBlock LayoutTableBlock(TableBox node, BlockLayoutRequest request)
    {
        ApplyTableBlockLayout(node, request, LayoutChildBlocks);
        return PublishResolvedBlock(node);
    }

    private void ApplyTableBlockLayout(
        TableBox node,
        BlockLayoutRequest request,
        Func<BlockBox, float, float, float, float, float> layoutChildBlocks)
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
                BoxNodePathBuilder.Build(node),
                result.UnsupportedStructureKind ?? "unsupported-table-structure",
                result.UnsupportedReason ?? "Unsupported table structure.",
                result.RowCount,
                result.RequestedWidth,
                result.ResolvedWidth,
                groupContexts: BuildTableGroupContexts(node),
                diagnosticsSink: _diagnosticsSink);

            TablePlacementApplier.ApplyUnsupportedPlaceholder(node, x, y, result.ResolvedWidth, margin);
            return;
        }

        TableLayoutDiagnostics.EmitSupportedTable(
            BoxNodePathBuilder.Build(node),
            result.Rows.Count,
            result.DerivedColumnCount,
            result.RequestedWidth,
            result.ResolvedWidth,
            BuildTableRowContexts(result),
            BuildTableCellContexts(result),
            BuildTableColumnContexts(result),
            BuildTableGroupContexts(node),
            diagnosticsSink: _diagnosticsSink);

        _tablePlacementApplier.ApplySupported(node, result, x, y, margin, layoutChildBlocks);
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

    private PublishedBlock PublishResolvedBlock(BlockBox block)
    {
        ArgumentNullException.ThrowIfNull(block);

        if (_publishedBlocks.TryGetValue(block, out var existing))
        {
            return existing;
        }

        var geometry = block.UsedGeometry ?? throw new InvalidOperationException(
            $"Published layout requires UsedGeometry for '{BoxNodePathBuilder.Build(block)}'.");
        var children = PublishResolvedChildren(block);
        var inlineLayout = CreatePublishedInlineLayout(block.InlineLayout);
        var identity = CreatePublishedIdentity(block);

        return CachePublishedBlock(
            block,
            PublishedBlockFactory.CreateBlock(
                block,
                identity,
                geometry,
                inlineLayout,
                children));
    }

    private IReadOnlyList<PublishedBlock> PublishResolvedChildren(BlockBox block)
    {
        var children = new List<PublishedBlock>();
        foreach (var child in BoxNodeTraversal.EnumerateBlockChildren(block))
        {
            if (InlineFlowClassifier.IsInlineFlowMember(child))
            {
                continue;
            }

            children.Add(PublishResolvedBlock(child));
        }

        return children;
    }

    private PublishedBlock PublishInlineObjectContent(BlockBox block)
    {
        if (_publishedBlocks.TryGetValue(block, out var existing))
        {
            return existing;
        }

        var geometry = block.UsedGeometry ?? throw new InvalidOperationException(
            $"Published inline object requires UsedGeometry for '{BoxNodePathBuilder.Build(block)}'.");
        var inlineLayout = CreatePublishedInlineLayout(block.InlineLayout);
        var identity = CreatePublishedIdentity(block);

        return CachePublishedBlock(
            block,
            PublishedBlockFactory.CreateBlock(
                block,
                identity,
                geometry,
                inlineLayout,
                children: []));
    }

    private PublishedInlineLayout? CreatePublishedInlineLayout(InlineLayoutResult? inlineLayout)
    {
        if (inlineLayout is null)
        {
            return null;
        }

        return new PublishedInlineLayout(
            inlineLayout.Segments.Select(CreatePublishedInlineSegment).ToArray(),
            inlineLayout.TotalHeight,
            inlineLayout.MaxLineWidth);
    }

    private PublishedInlineFlowSegment CreatePublishedInlineSegment(InlineFlowSegmentLayout segment)
    {
        return new PublishedInlineFlowSegment(
            segment.Lines.Select(CreatePublishedInlineLine).ToArray(),
            segment.Top,
            segment.Height);
    }

    private PublishedInlineLine CreatePublishedInlineLine(InlineLineLayout line)
    {
        return new PublishedInlineLine(
            line.LineIndex,
            line.Rect,
            line.OccupiedRect,
            line.BaselineY,
            line.LineHeight,
            line.TextAlign,
            line.Items.Select(CreatePublishedInlineItem).ToArray());
    }

    private PublishedInlineItem CreatePublishedInlineItem(InlineLineItemLayout item)
    {
        return item switch
        {
            InlineTextItemLayout text => new PublishedInlineTextItem(
                text.Order,
                text.Rect,
                text.Runs.ToArray(),
                text.Sources
                    .Select(source => PublishedBlockFactory.CreateInlineSource(
                        source,
                        GetPublishedSourceOrder(source)))
                    .ToArray()),
            InlineObjectItemLayout obj => new PublishedInlineObjectItem(
                obj.Order,
                obj.Rect,
                PublishInlineObjectContent(obj.ContentBox)),
            _ => throw new NotSupportedException(
                $"Unsupported inline layout item '{item.GetType().Name}'.")
        };
    }

    private PublishedBlock CachePublishedBlock(BlockBox block, PublishedBlock published)
    {
        _publishedBlocks[block] = published;
        return published;
    }

    private PublishedBlockIdentity CreatePublishedIdentity(BoxNode node)
    {
        return PublishedBlockFactory.CreateIdentity(node, GetPublishedSourceOrder(node));
    }

    private int GetPublishedSourceOrder(BoxNode node)
    {
        if (_publishedSourceOrders.TryGetValue(node, out var sourceOrder))
        {
            return sourceOrder;
        }

        sourceOrder = _nextPublishedSourceOrder++;
        _publishedSourceOrders.Add(node, sourceOrder);
        return sourceOrder;
    }

    private void ResetPublishedLayoutState()
    {
        _publishedSourceOrders.Clear();
        _publishedBlocks.Clear();
        _nextPublishedSourceOrder = 0;
    }

    private static float AdvanceCursorPastUsedGeometry(BlockBox block)
    {
        var geometry = block.UsedGeometry ?? throw new InvalidOperationException(
            $"Block layout cursor advance requires UsedGeometry for '{BoxNodePathBuilder.Build(block)}'.");

        return VerticalFlowPolicy.AdvanceCursorPast(geometry.Y, geometry.Height);
    }

    private static FormattingContextKind ResolveFormattingContext(BlockBox block) =>
        block.IsInlineBlockContext ? FormattingContextKind.InlineBlock : FormattingContextKind.Block;

    private readonly record struct BlockFlowLayoutResult(
        float ContentHeight,
        IReadOnlyList<PublishedBlock> PublishedChildren,
        PublishedInlineLayout? PublishedInlineLayout,
        IReadOnlyList<PublishedBlockFlowItem> PublishedFlow);

}
