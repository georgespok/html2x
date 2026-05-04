using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;

internal sealed class InlineFlowBuffer
{
    private readonly List<BoxNode> _nodes = [];

    public int Count => _nodes.Count;

    public IReadOnlyList<BoxNode> Nodes => _nodes;

    public bool TryQueue(BoxNode node)
    {
        if (!InlineFlowClassifier.IsInlineFlowMember(node))
        {
            return false;
        }

        _nodes.Add(node);
        return true;
    }

    public void Clear()
    {
        _nodes.Clear();
    }
}
