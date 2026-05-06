using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Test.Builders;

/// <summary>
///     Fluent builder for BlockBox graphs with explicit navigation.
///     Use Block/Inline to descend; Up() to go back; BuildRoot() to materialize the root.
/// </summary>
internal sealed class BlockBoxBuilder
{
    private readonly BlockBox _block;
    private readonly BlockBoxBuilder? _parent;

    public BlockBoxBuilder()
        : this(new(BoxRole.Block), null)
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
            Padding = new(top, right, bottom, left)
        };
        return this;
    }

    public BlockBoxBuilder WithMargin(float top = 0, float right = 0, float bottom = 0, float left = 0)
    {
        _block.Style = _block.Style with
        {
            Margin = new(top, right, bottom, left)
        };
        return this;
    }

    public BlockBoxBuilder WithTextAlign(string textAlign)
    {
        _block.TextAlign = textAlign;
        _block.Style = _block.Style with { TextAlign = textAlign };
        return this;
    }

    public BlockBoxBuilder Inline(string textContent, ComputedStyle? style = null)
    {
        _block.Children.Add(new InlineBox(BoxRole.Inline)
        {
            TextContent = textContent,
            Style = style ?? new ComputedStyle()
        });
        return this;
    }

    public BlockBoxBuilder Block(float x, float y, float width, float height, float fontSize = 12,
        ComputedStyle? style = null)
    {
        var resolvedStyle = style ?? new ComputedStyle { FontSizePt = fontSize };
        var block = new BlockBox(BoxRole.Block)
        {
            Style = resolvedStyle,
            Parent = _block
        };
        block.UsedGeometry = UsedGeometryRules.FromBorderBox(
            new(x, y, width, height),
            resolvedStyle.Padding.Safe(),
            Spacing.FromBorderEdges(resolvedStyle.Borders).Safe(),
            markerOffset: block.MarkerOffset);

        return Attach(block);
    }

    public BlockBoxBuilder Block(ComputedStyle? style = null) =>
        Attach(new(BoxRole.Block)
        {
            Style = style ?? new ComputedStyle(),
            Parent = _block
        });

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

    private BlockBoxBuilder Attach(BlockBox child)
    {
        _block.Children.Add(child);
        return new(child, this);
    }
}