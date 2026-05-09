using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Diagnostics;

namespace Html2x.LayoutEngine.Geometry.Publishing;

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

        if (_blocks.TryGetValue(result.Block, out var existing))
        {
            return existing;
        }

        if (result.InlineLayout is not null || result.Children.Count > 0 || result.Flow is not null)
        {
            var children = result.Children
                .Select(WriteRuleResult)
                .ToArray();

            return WriteBlock(
                result.Block,
                CreateInlineLayout(result.InlineLayout),
                children,
                CreateFlow(result.Flow));
        }

        return WriteResolvedBlock(result.Block);
    }

    public PublishedBlock WriteBlock(
        BlockBox block,
        InlineLayoutResult? inlineLayout,
        IReadOnlyList<BlockLayoutRuleResult> children,
        IReadOnlyList<BlockFlowItemLayout>? flow = null)
    {
        ArgumentNullException.ThrowIfNull(children);

        var publishedChildren = children
            .Select(WriteRuleResult)
            .ToArray();

        return WriteBlock(
            block,
            CreateInlineLayout(inlineLayout),
            publishedChildren,
            CreateFlow(flow));
    }

    private PublishedBlock WriteBlock(
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

    private IReadOnlyList<PublishedBlockFlowItem>? CreateFlow(IReadOnlyList<BlockFlowItemLayout>? flow)
    {
        if (flow is null)
        {
            return null;
        }

        return flow
            .Select(CreateFlowItem)
            .ToArray();
    }

    private PublishedBlockFlowItem CreateFlowItem(BlockFlowItemLayout item) =>
        item switch
        {
            BlockChildFlowItemLayout child => new PublishedChildBlockItem(
                child.Order,
                WriteRuleResult(child.Child)),
            InlineSegmentFlowItemLayout segment => new PublishedInlineFlowSegmentItem(
                segment.Order,
                CreateInlineSegment(segment.Segment)),
            _ => throw new NotSupportedException(
                $"Unsupported block flow item '{item.GetType().Name}'.")
        };

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
