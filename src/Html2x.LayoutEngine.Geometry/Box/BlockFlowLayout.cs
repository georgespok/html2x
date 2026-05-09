using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Owns normal block-flow sequencing for laid-out block content.
/// </summary>
internal sealed class BlockFlowLayout(
    InlineFlowLayout inlineFlowLayout,
    MarginCollapseRules marginCollapseRules,
    LayoutBoxStateWriter stateWriter,
    Func<BlockBox, BlockLayoutRequest, BlockLayoutRuleResult> layoutBlock,
    IDiagnosticsSink? diagnosticsSink = null)
{
    private readonly InlineFlowLayout _inlineFlowLayout =
        inlineFlowLayout ?? throw new ArgumentNullException(nameof(inlineFlowLayout));

    private readonly Func<BlockBox, BlockLayoutRequest, BlockLayoutRuleResult> _layoutBlock =
        layoutBlock ?? throw new ArgumentNullException(nameof(layoutBlock));

    private readonly MarginCollapseRules _marginCollapseRules =
        marginCollapseRules ?? throw new ArgumentNullException(nameof(marginCollapseRules));

    private readonly LayoutBoxStateWriter _stateWriter =
        stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));

    public BlockFlowLayoutResult Layout(BlockFlowLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parent = request.Parent;
        var state = new BlockFlowLayoutState(request.CursorY);
        var pendingInlineFlow = new InlineFlowBuffer();
        var inlineSegments = new List<InlineFlowSegmentLayout>();
        var children = new List<BlockLayoutRuleResult>();
        var flow = new List<BlockFlowItemLayout>();

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
                state,
                flow);

            if (parent.Children[i] is not BlockBox childBlock)
            {
                continue;
            }

            var marginTop = childBlock.Style.Margin.Safe().Top;
            var collapsedTop = VerticalFlowPolicy.CollapseTopMargin(
                _marginCollapseRules,
                state.PreviousBottomMargin,
                marginTop,
                ResolveFormattingContext(parent),
                GeometryDiagnosticNames.Consumers.BlockBoxLayout,
                diagnosticsSink);
            var childRequest = new BlockLayoutRequest(
                request.ContentX,
                state.CurrentY,
                request.ContentWidth,
                request.ParentContentTop,
                state.PreviousBottomMargin,
                collapsedTop);
            var childResult = _layoutBlock(childBlock, childRequest);
            children.Add(childResult);
            flow.Add(new BlockChildFlowItemLayout(state.ReserveFlowOrder(), childResult));

            parent.Children[i] = childBlock;
            state.CurrentY = AdvanceCursorPastUsedGeometry(childBlock);
            state.PreviousBottomMargin = childBlock.Margin.Bottom;
        }

        FlushInlineFlow(
            parent,
            pendingInlineFlow,
            inlineSegments,
            request.ContentX,
            request.ContentWidth,
            state,
            flow);

        var contentHeight =
            VerticalFlowPolicy.ResolveStackHeight(state.CurrentY, state.PreviousBottomMargin, request.CursorY);
        _stateWriter.ApplyInlineLayout(parent, new(inlineSegments, contentHeight, state.MaxLineWidth));

        return new(
            contentHeight,
            children,
            new(inlineSegments, contentHeight, state.MaxLineWidth),
            flow);
    }

    public BlockStackLayoutResult LayoutStack(BlockStackLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blocks = new List<BlockBox>();
        var results = new List<BlockLayoutRuleResult>();
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
                _marginCollapseRules,
                previousBottomMargin,
                marginTop,
                FormattingContextKind.Block,
                GeometryDiagnosticNames.Consumers.BlockBoxLayout,
                diagnosticsSink);
            var result = _layoutBlock(
                block,
                new(
                    request.ContentX,
                    currentY,
                    request.ContentWidth,
                    request.ContentY,
                    previousBottomMargin,
                    collapsedTop));

            blocks.Add(block);
            results.Add(result);
            currentY = AdvanceCursorPastUsedGeometry(block);
            previousBottomMargin = block.Margin.Bottom;
        }

        return new(blocks, results);
    }

    internal static float AdvanceCursorPastUsedGeometry(BlockBox block)
    {
        var geometry = block.UsedGeometry ?? throw new InvalidOperationException(
            $"Block layout cursor advance requires UsedGeometry for '{BoxNodePath.Build(block)}'.");

        return VerticalFlowPolicy.AdvanceCursorPast(geometry.Y, geometry.Height);
    }

    private void FlushInlineFlow(
        BlockBox blockContext,
        InlineFlowBuffer pendingInlineFlow,
        List<InlineFlowSegmentLayout> inlineSegments,
        float contentX,
        float contentWidth,
        BlockFlowLayoutState state,
        ICollection<BlockFlowItemLayout> flow)
    {
        if (pendingInlineFlow.Count == 0)
        {
            return;
        }

        var contentTop = state.CurrentY + state.PreviousBottomMargin;
        var segmentBlock = CreateInlineSegmentBlock(blockContext, pendingInlineFlow.Nodes);
        var inlineLayout = _inlineFlowLayout.LayoutInlineFlow(
            segmentBlock,
            new(
                contentX,
                contentTop,
                contentWidth,
                state.IncludeSyntheticListMarker));

        pendingInlineFlow.Clear();
        state.IncludeSyntheticListMarker = false;
        state.PreviousBottomMargin = 0f;

        if (inlineLayout.Segments.Count > 0)
        {
            inlineSegments.AddRange(inlineLayout.Segments);
            foreach (var segment in inlineLayout.Segments)
            {
                flow.Add(new InlineSegmentFlowItemLayout(state.ReserveFlowOrder(), segment));
            }
        }

        state.CurrentY = contentTop + inlineLayout.TotalHeight;
        state.MaxLineWidth = Math.Max(state.MaxLineWidth, inlineLayout.MaxLineWidth);
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

    private sealed class BlockFlowLayoutState(float currentY)
    {
        private int _nextFlowOrder;

        public float CurrentY { get; set; } = currentY;

        public float PreviousBottomMargin { get; set; }

        public float MaxLineWidth { get; set; }

        public bool IncludeSyntheticListMarker { get; set; } = true;

        public int ReserveFlowOrder() => _nextFlowOrder++;
    }
}
