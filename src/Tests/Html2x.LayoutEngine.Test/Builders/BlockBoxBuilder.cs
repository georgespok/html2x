using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Test.Builders;

/// <summary>
/// Fluent builder for BlockBox graphs with explicit navigation.
/// Use Block/Inline to descend; Up() to go back; BuildRoot() to materialize the root.
/// </summary>
internal sealed class BlockBoxBuilder
{
    private readonly BlockBox _block;
    private readonly BlockBoxBuilder? _parent;
    private Spacing? _pageMargins;

    public BlockBoxBuilder()
        : this(new BlockBox(), parent: null)
    {
    }

    internal BlockBoxBuilder(BlockBox block, BlockBoxBuilder? parent)
    {
        _block = block ?? throw new ArgumentNullException(nameof(block));
        _parent = parent;
    }

    public BlockBoxBuilder WithPadding(float top = 0, float right = 0, float bottom = 0, float left = 0)
    {
        _block.Style = _block.Style with
        {
            Padding = new Spacing(top, right, bottom, left)
        };
        return this;
    }

    // Compatibility wrappers
    public BlockBoxBuilder AddInline(string textContent, ComputedStyle? style = null) => Inline(textContent, style);
    public BlockBoxBuilder AddBlockChild(float x, float y, float width, float height, float fontSize = 12, ComputedStyle? style = null) =>
        Block(x, y, width, height, fontSize, style);
    public BlockBoxBuilder AddBlockChild(ComputedStyle? style = null) => Block(style);
    public BlockBoxBuilder AddBlock(float x, float y, float width, float height, float fontSize = 12, ComputedStyle? style = null) =>
        Block(x, y, width, height, fontSize, style);
    public BlockBoxBuilder AddBlock(ComputedStyle? style = null) => Block(style);

    public BlockBoxBuilder WithMargin(float top = 0, float right = 0, float bottom = 0, float left = 0)
    {
        _block.Style = _block.Style with
        {
            Margin = new Spacing(top, right, bottom, left)
        };
        return this;
    }

    public BlockBoxBuilder WithTextAlign(string textAlign)
    {
        _block.TextAlign = textAlign;
        _block.Style = _block.Style with { TextAlign = textAlign };
        return this;
    }

    public BlockBoxBuilder WithPageMargins(float top = 0, float right = 0, float bottom = 0, float left = 0)
    {
        if (_parent is not null)
        {
            return this;
        }

        _pageMargins = new Spacing(top, right, bottom, left);
        return this;
    }

    public BlockBoxBuilder Inline(string textContent, ComputedStyle? style = null)
    {
        _block.Children.Add(new InlineBox
        {
            TextContent = textContent,
            Style = style ?? new ComputedStyle()
        });
        return this;
    }

    public BlockBoxBuilder Block(float x, float y, float width, float height, float fontSize = 12, ComputedStyle? style = null)
    {
        return Attach(new BlockBox
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Style = style ?? new ComputedStyle { FontSizePt = fontSize },
            Parent = _block
        });
    }

    public BlockBoxBuilder Block(ComputedStyle? style = null)
    {
        return Attach(new BlockBox
        {
            Style = style ?? new ComputedStyle(),
            Parent = _block
        });
    }

    public BlockBoxBuilder Up() => _parent ?? this;

    public BlockBox Build() => _block;

    public BlockBox BuildRoot()
    {
        var current = this;
        while (current._parent is not null)
        {
            current = current._parent;
        }

        return current._block;
    }

    public BoxTree BuildTree()
    {
        var root = BuildRoot();
        var tree = new BoxTree();

        if (_pageMargins.HasValue)
        {
            tree.Page.Margin = _pageMargins.Value;
        }

        foreach (var child in root.Children.OfType<BlockBox>())
        {
            tree.Blocks.Add(child);
        }

        return tree;
    }

    private BlockBoxBuilder Attach(BlockBox child)
    {
        _block.Children.Add(child);
        return new BlockBoxBuilder(child, this);
    }
}
