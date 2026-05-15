using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Test;

internal static class InlineBlockBoxTree
{
    public static InlineBox Create(ComputedStyle? style = null) =>
        new(BoxRole.InlineBlock)
        {
            Style = style ?? new()
        };

    public static ImageBox AddImage(InlineBox inlineBlock, string src, ComputedStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(inlineBlock);

        var image = new ImageBox(BoxRole.Block)
        {
            Parent = inlineBlock,
            Src = src,
            Style = style ?? new()
        };
        inlineBlock.AddChild(image);

        return image;
    }

    public static BlockBox AddContentBox(
        InlineBox inlineBlock,
        ComputedStyle? style = null,
        bool isAnonymous = false)
    {
        ArgumentNullException.ThrowIfNull(inlineBlock);

        var contentBox = new BlockBox(BoxRole.Block)
        {
            Parent = inlineBlock,
            EstablishesInlineBlockFormattingContext = true,
            IsAnonymous = isAnonymous,
            Style = style ?? new()
        };
        inlineBlock.AddChild(contentBox);

        return contentBox;
    }

    public static InlineBox AddInline(BlockBox parent, string textContent, ComputedStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var inline = new InlineBox(BoxRole.Inline)
        {
            Parent = parent,
            Style = style ?? parent.Style,
            TextContent = textContent
        };
        parent.AddChild(inline);

        return inline;
    }

    public static BlockBox AddBlock(BlockBox parent, ComputedStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var block = new BlockBox(BoxRole.Block)
        {
            Parent = parent,
            Style = style ?? new()
        };
        parent.AddChild(block);

        return block;
    }
}
