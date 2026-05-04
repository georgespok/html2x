namespace Html2x.LayoutEngine.Box.Publishing;

using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Contracts.Style;

/// <summary>
/// Publishes resolved mutable box state into immutable published layout facts.
/// </summary>
internal sealed class PublishedLayoutPublisher
{
    private readonly Dictionary<BoxNode, int> _sourceOrders = [];
    private readonly Dictionary<BlockBox, PublishedBlock> _blocks = [];
    private int _nextSourceOrder;

    public void Reset()
    {
        _sourceOrders.Clear();
        _blocks.Clear();
        _nextSourceOrder = 0;
    }

    public PublishedBlock PublishResolvedBlock(BlockBox block)
    {
        ArgumentNullException.ThrowIfNull(block);

        if (_blocks.TryGetValue(block, out var existing))
        {
            return existing;
        }

        var geometry = block.UsedGeometry ?? throw new InvalidOperationException(
            $"Published layout requires UsedGeometry for '{BoxNodePathBuilder.Build(block)}'.");
        var children = PublishResolvedChildren(block);
        var inlineLayout = CreateInlineLayout(block.InlineLayout);

        return PublishBlock(block, inlineLayout, children);
    }

    public PublishedBlock PublishBlock(
        BlockBox block,
        PublishedInlineLayout? inlineLayout,
        IReadOnlyList<PublishedBlock> children,
        IReadOnlyList<PublishedBlockFlowItem>? flow = null)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(children);

        var geometry = block.UsedGeometry ?? throw new InvalidOperationException(
            $"Published layout requires UsedGeometry for '{BoxNodePathBuilder.Build(block)}'.");

        var published = PublishedBlockFactory.CreateBlock(
            block,
            PublishedBlockFactory.CreateIdentity(block, GetSourceOrder(block)),
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

    public PublishedInlineFlowSegment CreateInlineSegment(InlineFlowSegmentLayout segment)
    {
        return new PublishedInlineFlowSegment(
            segment.Lines.Select(CreateInlineLine).ToArray(),
            segment.Top,
            segment.Height);
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
        if (_blocks.TryGetValue(block, out var existing))
        {
            return existing;
        }

        var geometry = block.UsedGeometry ?? throw new InvalidOperationException(
            $"Published inline object requires UsedGeometry for '{BoxNodePathBuilder.Build(block)}'.");
        var inlineLayout = CreateInlineLayout(block.InlineLayout);

        var published = PublishedBlockFactory.CreateBlock(
            block,
            PublishedBlockFactory.CreateIdentity(block, GetSourceOrder(block)),
            geometry,
            inlineLayout,
            children: []);

        _blocks[block] = published;
        return published;
    }

    private PublishedInlineLine CreateInlineLine(InlineLineLayout line)
    {
        return new PublishedInlineLine(
            line.LineIndex,
            line.Rect,
            line.OccupiedRect,
            line.BaselineY,
            line.LineHeight,
            line.TextAlign,
            line.Items.Select(CreateInlineItem).ToArray());
    }

    private PublishedInlineItem CreateInlineItem(InlineLineItemLayout item)
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
                        GetSourceOrder(source)))
                    .ToArray()),
            InlineObjectItemLayout obj => new PublishedInlineObjectItem(
                obj.Order,
                obj.Rect,
                PublishInlineObjectContent(obj.ContentBox)),
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
