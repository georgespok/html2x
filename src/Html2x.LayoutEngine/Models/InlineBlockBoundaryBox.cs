namespace Html2x.LayoutEngine.Models;

public sealed class InlineBlockBoundaryBox(InlineBox sourceInline, BlockBox sourceContentBox)
    : BlockBox(DisplayRole.Block)
{
    public InlineBox SourceInline { get; } = sourceInline ?? throw new ArgumentNullException(nameof(sourceInline));

    public BlockBox SourceContentBox { get; } = sourceContentBox ?? throw new ArgumentNullException(nameof(sourceContentBox));
}
