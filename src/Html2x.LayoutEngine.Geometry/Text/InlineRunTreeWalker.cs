using Html2x.LayoutEngine.Geometry.Box;

namespace Html2x.LayoutEngine.Geometry.Text;

internal sealed class InlineRunTreeWalker(InlineRunCollector collector)
{
    private readonly InlineRunCollector _collector = collector ?? throw new ArgumentNullException(nameof(collector));

    public void CollectInlineFlow(IEnumerable<BoxNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        foreach (var node in nodes)
        {
            CollectInlineFlowNode(node);
        }
    }

    public void CollectInlineObjectContent(BlockBox block)
    {
        ArgumentNullException.ThrowIfNull(block);

        CollectInlineObjectNodes(block.Children, block.Style);
        _collector.TrimBoundaryLineBreaks();
    }

    private void CollectInlineFlowNode(BoxNode node)
    {
        if (node is BlockBox block && InlineFlowRules.IsAnonymousInlineWrapper(block))
        {
            CollectInlineFlow(block.Children);
            return;
        }

        if (TryAppendInlineFlowRun(node))
        {
            return;
        }

        if (node is not InlineBox inline)
        {
            return;
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineFlowNode(childInline);
        }
    }

    private bool TryAppendInlineFlowRun(BoxNode node)
    {
        if (node is InlineBlockBoundaryBox boundary)
        {
            return _collector.TryAppendInlineBlockBoundaryRun(boundary);
        }

        if (node is not InlineBox inline)
        {
            return false;
        }

        return _collector.TryAppendInlineBlockRun(inline) ||
               _collector.TryAppendLineBreakRun(inline) ||
               _collector.TryAppendTextRun(inline);
    }

    private void CollectInlineObjectNodes(IEnumerable<BoxNode> nodes, ComputedStyle blockStyle)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case InlineBox inline:
                    CollectInlineObjectInline(inline, blockStyle);
                    break;
                case BlockBox blockChild:
                    CollectInlineObjectBlockChild(blockChild, blockStyle);
                    break;
                default:
                    if (node.Children.Count > 0)
                    {
                        CollectInlineObjectNodes(node.Children, blockStyle);
                    }

                    break;
            }
        }
    }

    private void CollectInlineObjectBlockChild(BlockBox blockChild, ComputedStyle parentStyle)
    {
        var runCountBeforeBoundary = _collector.Count;
        AppendBlockBoundaryBreak(parentStyle);
        var runCountAfterBoundary = _collector.Count;

        CollectInlineObjectNodes(blockChild.Children, blockChild.Style);

        if (_collector.Count > runCountAfterBoundary)
        {
            AppendBlockBoundaryBreak(parentStyle);
            return;
        }

        if (_collector.Count > runCountBeforeBoundary && _collector.LastKind == TextRunKind.LineBreak)
        {
            _collector.RemoveLast();
        }
    }

    private void CollectInlineObjectInline(InlineBox inline, ComputedStyle blockStyle)
    {
        if (_collector.TryAppendInlineBlockRun(inline))
        {
            return;
        }

        if (_collector.TryAppendLineBreakRun(inline, blockStyle))
        {
            return;
        }

        _ = _collector.TryAppendTextRun(inline);

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineObjectInline(childInline, blockStyle);
        }
    }

    private void AppendBlockBoundaryBreak(ComputedStyle style)
    {
        if (_collector.Count == 0 || _collector.LastKind == TextRunKind.LineBreak)
        {
            return;
        }

        _collector.AppendSyntheticLineBreakRun(style);
    }
}