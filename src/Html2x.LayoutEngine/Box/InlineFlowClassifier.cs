using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

internal static class InlineFlowClassifier
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
