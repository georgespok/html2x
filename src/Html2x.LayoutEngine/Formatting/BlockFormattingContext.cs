using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Formatting;

/// <summary>
/// Owns supported block formatting measurement and margin-collapse policy.
/// </summary>
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

    public BlockMeasurementBasis Measure(BlockBox box, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(box);

        var style = box.Style;
        var margin = style.Margin.Safe();
        var padding = style.Padding.Safe();
        var border = Spacing.FromBorderEdges(style.Borders).Safe();

        var borderBoxWidth = BoxDimensionResolver.ResolveBlockBorderBoxWidth(style, availableWidth, margin);
        var contentBoxWidth = BoxDimensionResolver.ResolveContentBoxWidth(borderBoxWidth, padding, border, box.MarkerOffset);

        return new BlockMeasurementBasis(
            margin,
            padding,
            border,
            borderBoxWidth,
            contentBoxWidth);
    }

    public float CollapseMargins(
        float previousBottomMargin,
        float nextTopMargin,
        FormattingContextKind contextKind,
        string consumerName,
        DiagnosticsSession? diagnosticsSession = null)
    {
        var collapsedTopMargin = Math.Max(previousBottomMargin, nextTopMargin);

        if (diagnosticsSession is not null)
        {
            diagnosticsSession.Events.Add(new DiagnosticsEvent
            {
                Type = DiagnosticsEventType.Trace,
                Name = "layout/margin-collapse",
                Payload = new MarginCollapsePayload
                {
                    PreviousBottomMargin = previousBottomMargin,
                    NextTopMargin = nextTopMargin,
                    CollapsedTopMargin = collapsedTopMargin,
                    Owner = nameof(BlockFormattingContext),
                    Consumer = consumerName,
                    FormattingContext = contextKind
                }
            });
        }

        return collapsedTopMargin;
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

    private float ResolveTotalWidth(BlockFormattingRequest request, IReadOnlyList<BlockBox> blocks)
    {
        var directChildren = request.RootBlock.Children.OfType<BlockBox>().ToList();
        var maxWidth = 0f;

        if (directChildren.Count > 0)
        {
            foreach (var child in directChildren)
            {
                maxWidth = Math.Max(maxWidth, ResolveObservedOrExplicitWidth(child));
            }
        }
        else
        {
            foreach (var block in blocks)
            {
                if (ReferenceEquals(block, request.RootBlock))
                {
                    continue;
                }

                maxWidth = Math.Max(maxWidth, ResolveObservedOrExplicitWidth(block));
            }
        }

        if (request.IsWidthUnbounded)
        {
            return maxWidth;
        }

        return Math.Min(request.AvailableWidth, maxWidth);
    }

    private static float ResolveObservedOrExplicitWidth(BlockBox block)
    {
        if (block.Width > 0f)
        {
            return block.Width;
        }

        if (block.Style.WidthPt.HasValue)
        {
            return Math.Max(0f, block.Style.WidthPt.Value);
        }

        return 0f;
    }

    private float ResolveTotalHeight(BlockFormattingRequest request, IReadOnlyList<BlockBox> blocks)
    {
        if (TryResolveSequentialHeight(request, out var sequentialHeight))
        {
            return Math.Max(Math.Max(0f, request.RootBlock.Height), sequentialHeight);
        }

        if (blocks.Count == 0)
        {
            return Math.Max(0f, ResolveBlockHeight(request.RootBlock, request));
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

    private bool TryResolveSequentialHeight(BlockFormattingRequest request, out float totalHeight)
    {
        var blockChildren = request.RootBlock.Children.OfType<BlockBox>().ToList();
        if (blockChildren.Count == 0)
        {
            totalHeight = 0f;
            return false;
        }

        totalHeight = 0f;
        for (var i = 0; i < blockChildren.Count; i++)
        {
            var current = blockChildren[i];
            totalHeight += ResolveBlockHeight(current, request);

            if (i == blockChildren.Count - 1)
            {
                continue;
            }

            var currentMarginBottom = ResolveMargin(current).Bottom;
            var next = blockChildren[i + 1];
            var nextMarginTop = ResolveMargin(next).Top;
            totalHeight += CollapseMargins(
                currentMarginBottom,
                nextMarginTop,
                request.ContextKind,
                request.ConsumerName,
                request.EmitDiagnostics ? request.DiagnosticsSession : null);
        }

        totalHeight += ResolveMargin(blockChildren[^1]).Bottom;
        return true;
    }

    private static float ResolveBlockHeight(BlockBox block, BlockFormattingRequest request)
    {
        if (block is TableBox table && request.TableHeightMeasurer is not null)
        {
            return Math.Max(0f, request.TableHeightMeasurer(table, request.AvailableWidth));
        }

        if (request.BlockHeightMeasurer is not null)
        {
            return Math.Max(0f, request.BlockHeightMeasurer(block, request.AvailableWidth));
        }

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
