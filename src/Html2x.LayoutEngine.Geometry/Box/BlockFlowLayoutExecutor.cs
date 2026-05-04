using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Box.Publishing;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Box;


/// <summary>
/// Owns normal block-flow sequencing for laid-out block content.
/// </summary>
internal sealed class BlockFlowLayoutExecutor
{
    private readonly InlineLayoutEngine _inlineEngine;
    private readonly IBlockFormattingContext _blockFormattingContext;
    private readonly PublishedLayoutPublisher _publisher;
    private readonly IDiagnosticsSink? _diagnosticsSink;
    private readonly Func<BlockBox, BlockLayoutRequest, PublishedBlock> _layoutBlock;

    public BlockFlowLayoutExecutor(
        InlineLayoutEngine inlineEngine,
        IBlockFormattingContext blockFormattingContext,
        PublishedLayoutPublisher publisher,
        Func<BlockBox, BlockLayoutRequest, PublishedBlock> layoutBlock,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        _inlineEngine = inlineEngine ?? throw new ArgumentNullException(nameof(inlineEngine));
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _layoutBlock = layoutBlock ?? throw new ArgumentNullException(nameof(layoutBlock));
        _diagnosticsSink = diagnosticsSink;
    }

    public BlockFlowLayoutResult Layout(BlockFlowLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parent = request.Parent;
        var currentY = request.CursorY;
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
                request.ContentX,
                request.ContentWidth,
                ref currentY,
                ref previousBottomMargin,
                ref maxLineWidth,
                ref includeSyntheticListMarker,
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
                request.ContentX,
                currentY,
                request.ContentWidth,
                request.ParentContentTop,
                previousBottomMargin,
                collapsedTop);
            var publishedChild = _layoutBlock(childBlock, childRequest);
            publishedChildren.Add(publishedChild);
            publishedFlow.Add(new PublishedChildBlockItem(nextFlowOrder++, publishedChild));

            parent.Children[i] = childBlock;
            currentY = AdvanceCursorPastUsedGeometry(childBlock);
            previousBottomMargin = childBlock.Margin.Bottom;
        }

        FlushInlineFlow(
            parent,
            pendingInlineFlow,
            inlineSegments,
            request.ContentX,
            request.ContentWidth,
            ref currentY,
            ref previousBottomMargin,
            ref maxLineWidth,
            ref includeSyntheticListMarker,
            publishedInlineSegments,
            publishedFlow,
            ref nextFlowOrder);

        var contentHeight = VerticalFlowPolicy.ResolveStackHeight(currentY, previousBottomMargin, request.CursorY);
        parent.InlineLayout = new InlineLayoutResult(inlineSegments, contentHeight, maxLineWidth);
        var publishedInlineLayout = new PublishedInlineLayout(publishedInlineSegments, contentHeight, maxLineWidth);

        return new BlockFlowLayoutResult(
            contentHeight,
            publishedChildren,
            publishedInlineLayout,
            publishedFlow);
    }

    public BlockStackLayoutResult LayoutStack(BlockStackLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blocks = new List<BlockBox>();
        var publishedBlocks = new List<PublishedBlock>();
        var currentY = request.ContentY;
        var previousBottomMargin = 0f;

        foreach (var child in request.Candidates)
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
            var publishedBlock = _layoutBlock(
                block,
                new BlockLayoutRequest(
                    request.ContentX,
                    currentY,
                    request.ContentWidth,
                    request.ContentY,
                    previousBottomMargin,
                    collapsedTop));

            blocks.Add(block);
            publishedBlocks.Add(publishedBlock);
            currentY = AdvanceCursorPastUsedGeometry(block);
            previousBottomMargin = block.Margin.Bottom;
        }

        return new BlockStackLayoutResult(blocks, publishedBlocks);
    }

    internal static float AdvanceCursorPastUsedGeometry(BlockBox block)
    {
        var geometry = block.UsedGeometry ?? throw new InvalidOperationException(
            $"Block layout cursor advance requires UsedGeometry for '{BoxNodePathBuilder.Build(block)}'.");

        return VerticalFlowPolicy.AdvanceCursorPast(geometry.Y, geometry.Height);
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
            foreach (var segment in inlineLayout.Segments)
            {
                var publishedSegment = _publisher.CreateInlineSegment(segment);
                publishedInlineSegments.Add(publishedSegment);
                publishedFlow.Add(new PublishedInlineFlowSegmentItem(nextFlowOrder++, publishedSegment));
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

    private static FormattingContextKind ResolveFormattingContext(BlockBox block) =>
        block.IsInlineBlockContext ? FormattingContextKind.InlineBlock : FormattingContextKind.Block;
}
