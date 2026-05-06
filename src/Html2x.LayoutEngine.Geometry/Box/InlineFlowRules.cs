namespace Html2x.LayoutEngine.Geometry.Box;

internal static class InlineFlowRules
{
    public static bool IsInlineFlowMember(BoxNode node)
    {
        return node switch
        {
            InlineBox => true,
            InlineBlockBoundaryBox => true,
            BlockBox block when IsAnonymousInlineWrapper(block) => true,
            _ => false
        };
    }

    public static bool IsAnonymousInlineWrapper(BlockBox block)
    {
        return block.IsAnonymous &&
               block.Children.Count > 0 &&
               block.Children.All(static child => child is InlineBox);
    }
}