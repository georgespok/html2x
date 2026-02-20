using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Formatting;

internal sealed class BlockFormattingContext : IBlockFormattingContext
{
    public BlockFormattingResult Format(BlockFormattingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blocks = new List<BlockBox>();
        CollectBlocksDepthFirst(request.RootBlock, blocks);

        var totalWidth = ResolveTotalWidth(request, blocks);
        var totalHeight = ResolveTotalHeight(request, blocks);
        float? baseline = request.ContextKind == FormattingContextKind.InlineBlock
            ? request.RootBlock.Height
            : null;

        return new BlockFormattingResult(blocks, totalWidth, totalHeight, baseline);
    }

    private static void CollectBlocksDepthFirst(BlockBox root, ICollection<BlockBox> output)
    {
        output.Add(root);

        foreach (var child in root.Children)
        {
            if (child is BlockBox childBlock)
            {
                CollectBlocksDepthFirst(childBlock, output);
            }
        }
    }

    private static float ResolveTotalWidth(BlockFormattingRequest request, IReadOnlyList<BlockBox> blocks)
    {
        var maxWidth = blocks.Count == 0 ? 0f : blocks.Max(static b => b.Width);

        if (request.IsWidthUnbounded)
        {
            return maxWidth;
        }

        return Math.Min(request.AvailableWidth, maxWidth);
    }

    private static float ResolveTotalHeight(BlockFormattingRequest request, IReadOnlyList<BlockBox> blocks)
    {
        if (IsInlineBlockScope(request) &&
            TryResolveInlineBlockSequentialHeight(request.RootBlock, out var inlineBlockHeight))
        {
            return Math.Max(Math.Max(0f, request.RootBlock.Height), inlineBlockHeight);
        }

        if (blocks.Count == 0)
        {
            return Math.Max(0f, request.RootBlock.Height);
        }

        var minY = request.RootBlock.Y;
        var maxY = request.RootBlock.Y + request.RootBlock.Height;

        foreach (var block in blocks)
        {
            var (top, bottom) = ResolveVerticalBounds(block);
            minY = Math.Min(minY, top);
            maxY = Math.Max(maxY, bottom);
        }

        return Math.Max(0f, maxY - minY);
    }

    private static bool IsInlineBlockScope(BlockFormattingRequest request) =>
        request.ContextKind == FormattingContextKind.InlineBlock;

    private static bool TryResolveInlineBlockSequentialHeight(BlockBox rootBlock, out float totalHeight)
    {
        var blockChildren = rootBlock.Children.OfType<BlockBox>().ToList();
        if (blockChildren.Count == 0)
        {
            totalHeight = 0f;
            return false;
        }

        totalHeight = 0f;
        for (var i = 0; i < blockChildren.Count; i++)
        {
            var current = blockChildren[i];
            totalHeight += ResolveBlockHeight(current);

            if (i == blockChildren.Count - 1)
            {
                continue;
            }

            var currentMarginBottom = ResolveMargin(current).Bottom;
            var next = blockChildren[i + 1];
            var nextMarginTop = ResolveMargin(next).Top;
            totalHeight += Math.Max(currentMarginBottom, nextMarginTop);
        }

        return true;
    }

    private static float ResolveBlockHeight(BlockBox block)
    {
        var padding = block.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(block.Style.Borders).Safe();

        if (block.Style.HeightPt is float explicitHeight)
        {
            return Math.Max(0f, explicitHeight) + padding.Vertical + border.Vertical;
        }

        return Math.Max(0f, block.Height);
    }

    private static Spacing ResolveMargin(BlockBox block)
    {
        var styleMargin = block.Style.Margin.Safe();
        if (HasAnySpacing(styleMargin))
        {
            return styleMargin;
        }

        return block.Margin.Safe();
    }

    private static bool HasAnySpacing(Spacing spacing)
    {
        return spacing.Left > 0f || spacing.Right > 0f || spacing.Top > 0f || spacing.Bottom > 0f;
    }

    private static (float Top, float Bottom) ResolveVerticalBounds(BlockBox block)
    {
        var margin = ResolveMargin(block);
        var top = block.Y - margin.Top;
        var bottom = block.Y + block.Height + margin.Bottom;
        return (top, bottom);
    }
}
