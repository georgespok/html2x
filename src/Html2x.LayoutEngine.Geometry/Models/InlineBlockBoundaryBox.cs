namespace Html2x.LayoutEngine.Models;

internal sealed class InlineBlockBoundaryBox(InlineBox sourceInline, BlockBox sourceContentBox)
    : BlockBox(BoxRole.Block)
{
    public InlineBox SourceInline { get; } = sourceInline ?? throw new ArgumentNullException(nameof(sourceInline));

    public BlockBox SourceContentBox { get; } = sourceContentBox ?? throw new ArgumentNullException(nameof(sourceContentBox));

    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return CopyBlockStateTo(new InlineBlockBoundaryBox(SourceInline, SourceContentBox)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
            SourceIdentity = SourceIdentity
        });
    }
}
