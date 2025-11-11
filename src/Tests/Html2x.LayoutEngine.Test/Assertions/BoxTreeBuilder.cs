using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine.Test.Assertions;

internal sealed class BoxTreeTestBuilder
{
    private readonly BoxTree _tree = new();

    public BoxTreeTestBuilder WithPageMargins(float top = 0, float right = 0, float bottom = 0, float left = 0)
    {
        _tree.Page.MarginTopPt = top;
        _tree.Page.MarginRightPt = right;
        _tree.Page.MarginBottomPt = bottom;
        _tree.Page.MarginLeftPt = left;
        return this;
    }

    public BoxTreeTestBuilder AddBlock(float x, float y, float width, float height, float fontSize = 12, ComputedStyle? style = null, Action<BlockBoxBuilder>? configure = null)
    {
        var block = new BlockBox
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Style = style ?? new ComputedStyle { FontSizePt = fontSize }
        };
        configure?.Invoke(new BlockBoxBuilder(block));
        _tree.Blocks.Add(block);
        return this;
    }

    public BoxTree Build() => _tree;

    public static implicit operator BoxTree(BoxTreeTestBuilder builder) => builder._tree;
}

internal sealed class BlockBoxBuilder(BlockBox block)
{
    public BlockBoxBuilder AddInline(string textContent, float fontSize = 12)
    {
        var inline = new InlineBox
        {
            Style = new ComputedStyle { FontSizePt = fontSize },
            TextContent = textContent
        };
        block.Children.Add(inline);
        return this;
    }
    
    public BlockBoxBuilder AddBlock(float x, float y, float width, float height, float fontSize = 12, ComputedStyle? style = null, Action<BlockBoxBuilder>? configure = null)
    {
        var child = new BlockBox
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Style = style ?? new ComputedStyle { FontSizePt = fontSize }
        };
        configure?.Invoke(new BlockBoxBuilder(child));
        block.Children.Add(child);
        return this;
    }
}

