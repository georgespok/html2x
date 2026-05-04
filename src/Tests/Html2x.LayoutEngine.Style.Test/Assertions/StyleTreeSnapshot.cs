using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Style;
using Shouldly;

namespace Html2x.LayoutEngine.Style.Test.Assertions;


internal static class StyleTreeSnapshot
{
    public static StyleSnapshot FromTree(StyleTree tree)
        => tree.Root is null
            ? new StyleSnapshot("empty")
            : FromNode(tree.Root);

    private static StyleSnapshot FromNode(StyleNode node)
        => new(
            Tag: node.Element.TagName.ToLowerInvariant(),
            Style: node.Style,
            Children: node.Children.Select(FromNode).ToList()
        );
}
