namespace Html2x.LayoutEngine.Geometry.InlineFlow;

internal sealed class InlineRunTreeWalker(InlineRunBuffer runBuffer)
{
    private readonly InlineRunBuffer _runBuffer = runBuffer ?? throw new ArgumentNullException(nameof(runBuffer));

    public void CollectInlineFlow(IEnumerable<BoxNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        foreach (var node in nodes)
        {
            CollectInlineFlowNode(node);
        }
    }

    public void CollectInlineBoxContent(BlockBox block)
    {
        ArgumentNullException.ThrowIfNull(block);

        CollectInlineBoxNodes(block.Children, block.Style);
        _runBuffer.TrimBoundaryLineBreaks();
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
            return _runBuffer.TryAppendInlineBlockBoundaryRun(boundary);
        }

        if (node is not InlineBox inline)
        {
            return false;
        }

        return _runBuffer.TryAppendInlineBlockRun(inline) ||
               _runBuffer.TryAppendLineBreakRun(inline) ||
               _runBuffer.TryAppendTextRun(inline);
    }

    private void CollectInlineBoxNodes(IEnumerable<BoxNode> nodes, ComputedStyle blockStyle)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case InlineBox inline:
                    CollectInlineBoxInline(inline, blockStyle);
                    break;
                case BlockBox blockChild:
                    CollectInlineBoxBlockChild(blockChild, blockStyle);
                    break;
                default:
                    if (node.Children.Count > 0)
                    {
                        CollectInlineBoxNodes(node.Children, blockStyle);
                    }

                    break;
            }
        }
    }

    private void CollectInlineBoxBlockChild(BlockBox blockChild, ComputedStyle parentStyle)
    {
        var runCountBeforeBoundary = _runBuffer.Count;
        AppendBlockBoundaryBreak(parentStyle);
        var runCountAfterBoundary = _runBuffer.Count;

        CollectInlineBoxNodes(blockChild.Children, blockChild.Style);

        if (_runBuffer.Count > runCountAfterBoundary)
        {
            AppendBlockBoundaryBreak(parentStyle);
            return;
        }

        if (_runBuffer.Count > runCountBeforeBoundary && _runBuffer.LastKind == TextRunKind.LineBreak)
        {
            _runBuffer.RemoveLast();
        }
    }

    private void CollectInlineBoxInline(InlineBox inline, ComputedStyle blockStyle)
    {
        if (_runBuffer.TryAppendInlineBlockRun(inline))
        {
            return;
        }

        if (_runBuffer.TryAppendLineBreakRun(inline, blockStyle))
        {
            return;
        }

        _ = _runBuffer.TryAppendTextRun(inline);

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineBoxInline(childInline, blockStyle);
        }
    }

    private void AppendBlockBoundaryBreak(ComputedStyle style)
    {
        if (_runBuffer.Count == 0 || _runBuffer.LastKind == TextRunKind.LineBreak)
        {
            return;
        }

        _runBuffer.AppendSyntheticLineBreakRun(style);
    }
}
