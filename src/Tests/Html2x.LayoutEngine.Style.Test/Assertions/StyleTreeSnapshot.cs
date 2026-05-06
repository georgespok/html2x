using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Style.Test.Assertions;

internal static class StyleTreeSnapshot
{
    public static StyleSnapshot FromTree(StyleTree tree)
        => tree.Root is null
            ? new("empty")
            : FromNode(tree.Root);

    private static StyleSnapshot FromNode(StyleNode node)
        => new(
            node.Element.TagName.ToLowerInvariant(),
            node.Style,
            node.Children.Select(FromNode).ToList()
        );
}