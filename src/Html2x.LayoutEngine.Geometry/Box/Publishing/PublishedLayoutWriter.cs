using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Diagnostics;

namespace Html2x.LayoutEngine.Geometry.Box.Publishing;

/// <summary>
///     Writes resolved mutable box state into immutable published layout facts.
/// </summary>
internal sealed class PublishedLayoutWriter
{
    private readonly Dictionary<BlockBox, PublishedBlock> _blocks = [];
    private readonly Dictionary<BoxNode, int> _sourceOrders = [];
    private int _nextSourceOrder;

    public void Reset()
    {
        _sourceOrders.Clear();
        _blocks.Clear();
        _nextSourceOrder = 0;
    }

    public PublishedBlock WriteResolvedBlock(BlockBox block)
    {
        ArgumentNullException.ThrowIfNull(block);

        if (_blocks.TryGetValue(block, out var existing))
        {
            return existing;
        }

        if (block.UsedGeometry == null)
        {
            throw new InvalidOperationException(
                $"Published layout requires UsedGeometry for '{BoxNodePath.Build(block)}'.");
        }

        var children = WriteResolvedChildren(block);
        var inlineLayout = CreateInlineLayout(block.InlineLayout);

        return WriteBlock(block, inlineLayout, children);
    }

    public PublishedBlock WriteRuleResult(BlockLayoutRuleResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.InlineLayout is not null || result.Children.Count > 0 || result.Flow is not null)
        {
            return WriteBlock(
                result.Block,
                result.InlineLayout,
                result.Children,
                result.Flow);
        }

        return WriteResolvedBlock(result.Block);
    }

    public PublishedBlock WriteBlock(
        BlockBox block,
        PublishedInlineLayout? inlineLayout,
        IReadOnlyList<PublishedBlock> children,
        IReadOnlyList<PublishedBlockFlowItem>? flow = null)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(children);

        var geometry = block.UsedGeometry ?? throw new InvalidOperationException(
            $"Published layout requires UsedGeometry for '{BoxNodePath.Build(block)}'.");

        var published = PublishedBlockFacts.CreateBlock(
            block,
            PublishedBlockFacts.CreateIdentity(block, GetSourceOrder(block)),
            geometry,
            inlineLayout,
            children,
            flow);

        _blocks[block] = published;
        return published;
    }

    public PublishedInlineLayout? CreateInlineLayout(InlineLayoutResult? inlineLayout)
    {
        if (inlineLayout is null)
        {
            return null;
        }

        return new PublishedInlineLayout(
            inlineLayout.Segments.Select(CreateInlineSegment).ToArray(),
            inlineLayout.TotalHeight,
            inlineLayout.MaxLineWidth);
    }

    public PublishedInlineLayout WriteInlineLayout(
        IReadOnlyList<PublishedInlineFlowSegment> segments,
        float contentHeight,
        float maxLineWidth)
    {
        ArgumentNullException.ThrowIfNull(segments);
        return new PublishedInlineLayout(segments, contentHeight, maxLineWidth);
    }

    public PublishedChildBlockItem WriteChildFlowItem(int order, PublishedBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return new PublishedChildBlockItem(order, block);
    }

    public PublishedInlineFlowWriteResult WriteInlineFlow(
        IReadOnlyList<InlineFlowSegmentLayout> segments,
        Func<int> reserveFlowOrder)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(reserveFlowOrder);

        if (segments.Count == 0)
        {
            return PublishedInlineFlowWriteResult.Empty;
        }

        var publishedSegments = new List<PublishedInlineFlowSegment>(segments.Count);
        var publishedFlow = new List<PublishedBlockFlowItem>(segments.Count);

        foreach (var segment in segments)
        {
            var publishedSegment = CreateInlineSegment(segment);
            publishedSegments.Add(publishedSegment);
            publishedFlow.Add(new PublishedInlineFlowSegmentItem(reserveFlowOrder(), publishedSegment));
        }

        return new(publishedSegments, publishedFlow);
    }

    private PublishedInlineFlowSegment CreateInlineSegment(InlineFlowSegmentLayout segment) =>
        new(
            segment.Lines.Select(CreateInlineLine).ToArray(),
            segment.Top,
            segment.Height);

    private IReadOnlyList<PublishedBlock> WriteResolvedChildren(BlockBox block)
    {
        var children = new List<PublishedBlock>();
        foreach (var child in BoxNodeTraversal.EnumerateBlockChildren(block))
        {
            if (InlineFlowRules.IsInlineFlowMember(child))
            {
                continue;
            }

            children.Add(WriteResolvedBlock(child));
        }

        return children;
    }

    private PublishedBlock WriteInlineBoxContent(BlockBox block)
    {
        if (_blocks.TryGetValue(block, out var existing))
        {
            return existing;
        }

        var geometry = block.UsedGeometry ?? throw new InvalidOperationException(
            $"Published inline box requires UsedGeometry for '{BoxNodePath.Build(block)}'.");
        var inlineLayout = CreateInlineLayout(block.InlineLayout);

        var published = PublishedBlockFacts.CreateBlock(
            block,
            PublishedBlockFacts.CreateIdentity(block, GetSourceOrder(block)),
            geometry,
            inlineLayout,
            []);

        _blocks[block] = published;
        return published;
    }

    private PublishedInlineLine CreateInlineLine(InlineLineLayout line) =>
        new(
            line.LineIndex,
            line.Rect,
            line.OccupiedRect,
            line.BaselineY,
            line.LineHeight,
            line.TextAlign,
            line.Items.Select(CreateInlineItem).ToArray());

    private PublishedInlineItem CreateInlineItem(InlineLineItemLayout item)
    {
        return item switch
        {
            InlineTextItemLayout text => new PublishedInlineTextItem(
                text.Order,
                text.Rect,
                text.Runs.ToArray(),
                text.Sources
                    .Select(source => PublishedBlockFacts.CreateInlineSource(
                        source,
                        GetSourceOrder(source)))
                    .ToArray()),
            InlineBoxItemLayout box => new PublishedInlineObjectItem(
                box.Order,
                box.Rect,
                WriteInlineBoxContent(box.ContentBox)),
            _ => throw new NotSupportedException(
                $"Unsupported inline layout item '{item.GetType().Name}'.")
        };
    }

    private int GetSourceOrder(BoxNode node)
    {
        if (_sourceOrders.TryGetValue(node, out var sourceOrder))
        {
            return sourceOrder;
        }

        sourceOrder = _nextSourceOrder++;
        _sourceOrders.Add(node, sourceOrder);
        return sourceOrder;
    }
}

internal readonly record struct PublishedInlineFlowWriteResult(
    IReadOnlyList<PublishedInlineFlowSegment> Segments,
    IReadOnlyList<PublishedBlockFlowItem> FlowItems)
{
    public static PublishedInlineFlowWriteResult Empty { get; } = new([], []);
}
