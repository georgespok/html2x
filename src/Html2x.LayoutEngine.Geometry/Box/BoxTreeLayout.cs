using Html2x.LayoutEngine.Contracts.Published;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Places a constructed box tree against page geometry and returns published layout facts.
/// </summary>
internal sealed class BoxTreeLayout(BlockBoxLayout blockBoxLayout)
{
    private readonly BlockBoxLayout _blockBoxLayout =
        blockBoxLayout ?? throw new ArgumentNullException(nameof(blockBoxLayout));

    public PublishedLayoutTree Layout(BoxNode boxRoot, PageBox page)
    {
        ArgumentNullException.ThrowIfNull(boxRoot);
        ArgumentNullException.ThrowIfNull(page);

        var pageFacts = new PublishedPage(page.Size, page.Margin);
        var blocks = ResolveTopLevelBlocks(boxRoot, page);

        return new(pageFacts, blocks);
    }

    private IReadOnlyList<PublishedBlock> ResolveTopLevelBlocks(BoxNode boxRoot, PageBox page)
    {
        var contentArea = PageContentArea.From(page.Size, page.Margin);
        var candidates = SelectTopLevelCandidates(boxRoot);
        return _blockBoxLayout.LayoutBlockStack(new(
            candidates,
            contentArea.X,
            contentArea.Y,
            contentArea.Width));
    }

    private static IReadOnlyList<BoxNode> SelectTopLevelCandidates(BoxNode boxRoot)
    {
        if (boxRoot is TableBox tableRoot)
        {
            return [tableRoot];
        }

        if (boxRoot is BlockBox rootBlock)
        {
            return IsInlineOnlyBlock(rootBlock)
                ? [rootBlock]
                : rootBlock.Children;
        }

        return [boxRoot];
    }

    private static bool IsInlineOnlyBlock(BlockBox block)
    {
        var hasInline = block.Children.Any(c => c is InlineBox);
        var hasBlockOrTable = block.Children.Any(static c => c is BlockBox);
        return hasInline && !hasBlockOrTable;
    }
}