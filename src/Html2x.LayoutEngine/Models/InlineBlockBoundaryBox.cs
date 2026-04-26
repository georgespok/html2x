namespace Html2x.LayoutEngine.Models;

public sealed class InlineBlockBoundaryBox(InlineBox sourceInline, BlockBox sourceContentBox)
    : BlockBox(DisplayRole.Block)
{
    public InlineBox SourceInline { get; } = sourceInline ?? throw new ArgumentNullException(nameof(sourceInline));

    public BlockBox SourceContentBox { get; } = sourceContentBox ?? throw new ArgumentNullException(nameof(sourceContentBox));

    protected override DisplayNode CloneShallowForParent(DisplayNode parent)
    {
        return new InlineBlockBoundaryBox(SourceInline, SourceContentBox)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Margin = Margin,
            Padding = Padding,
            TextAlign = TextAlign,
            MarkerOffset = MarkerOffset,
            UsedGeometry = UsedGeometry,
            IsAnonymous = IsAnonymous,
            IsInlineBlockContext = IsInlineBlockContext
        };
    }
}
