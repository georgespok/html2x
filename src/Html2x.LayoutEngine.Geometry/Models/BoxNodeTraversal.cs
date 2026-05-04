namespace Html2x.LayoutEngine.Geometry.Models;

internal static class BoxNodeTraversal
{
    public static IEnumerable<BoxNode> EnumerateFlowChildren(BoxNode parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        foreach (var child in parent.Children)
        {
            foreach (var expanded in ExpandTransparentContainers(child))
            {
                yield return expanded;
            }
        }
    }

    public static IEnumerable<BlockBox> EnumerateBlockChildren(BoxNode parent)
    {
        foreach (var child in EnumerateFlowChildren(parent))
        {
            if (child is BlockBox block)
            {
                yield return block;
            }
        }
    }

    private static IEnumerable<BoxNode> ExpandTransparentContainers(BoxNode node)
    {
        if (node is TableSectionBox section)
        {
            foreach (var child in section.Children)
            {
                foreach (var expanded in ExpandTransparentContainers(child))
                {
                    yield return expanded;
                }
            }

            yield break;
        }

        yield return node;
    }
}
