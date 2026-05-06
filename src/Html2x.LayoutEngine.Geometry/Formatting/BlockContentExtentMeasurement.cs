using Html2x.LayoutEngine.Geometry.Box;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Formatting;

/// <summary>
///     Measures block content extents without mutating source boxes.
/// </summary>
internal sealed class BlockContentExtentMeasurement
{
    private readonly BlockFlowMeasurement _flowMeasurement;

    public BlockContentExtentMeasurement()
        : this(new())
    {
    }

    internal BlockContentExtentMeasurement(MarginCollapseRules marginCollapseRules)
    {
        MarginCollapseRules = marginCollapseRules ?? throw new ArgumentNullException(nameof(marginCollapseRules));
        _flowMeasurement = new(MarginCollapseRules);
    }

    public MarginCollapseRules MarginCollapseRules { get; }

    public BlockContentExtentMeasurementResult Measure(BlockContentExtentMeasurementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blocks = new List<BlockBox>();
        CollectBlocksDepthFirst(request.RootBlock, blocks);

        var totalWidth = ResolveTotalWidth(request, blocks);
        var totalHeight = ResolveTotalHeight(request, blocks);
        float? baseline = request.ContextKind == FormattingContextKind.InlineBlock
            ? ResolvePublishedOrZeroHeight(request.RootBlock)
            : null;

        return new(blocks, totalWidth, totalHeight, baseline);
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

    private float ResolveTotalWidth(BlockContentExtentMeasurementRequest request, IReadOnlyList<BlockBox> blocks)
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
        if (block.UsedGeometry is { } geometry)
        {
            return geometry.Width;
        }

        if (block.Style.WidthPt.HasValue)
        {
            return Math.Max(0f, block.Style.WidthPt.Value);
        }

        return 0f;
    }

    private float ResolveTotalHeight(BlockContentExtentMeasurementRequest request, IReadOnlyList<BlockBox> blocks)
    {
        var sequentialHeight = ResolveSequentialHeight(request);
        if (sequentialHeight.HasValue)
        {
            return Math.Max(Math.Max(0f, ResolvePublishedOrZeroHeight(request.RootBlock)), sequentialHeight.Value);
        }

        if (blocks.Count == 0)
        {
            return Math.Max(0f, BlockFlowMeasurement.ResolveBlockHeight(
                request.RootBlock,
                request.AvailableWidth,
                request.BlockHeightMeasurer,
                request.TableHeightMeasurer));
        }

        var rootY = ResolvePublishedOrZeroY(request.RootBlock);
        var minY = rootY;
        var maxY = rootY + ResolvePublishedOrZeroHeight(request.RootBlock);

        foreach (var block in blocks)
        {
            var (top, bottom) = ResolveVerticalBounds(block);
            minY = Math.Min(minY, top);
            maxY = Math.Max(maxY, bottom);
        }

        return Math.Max(0f, maxY - minY);
    }

    private float? ResolveSequentialHeight(BlockContentExtentMeasurementRequest request)
    {
        var result = _flowMeasurement.MeasureStackedChildren(
            request.RootBlock.Children,
            request.AvailableWidth,
            request.BlockHeightMeasurer,
            request.TableHeightMeasurer,
            request.EmitDiagnostics ? request.DiagnosticsSink : null,
            request.ContextKind,
            request.ConsumerName);

        return result.HasBlocks ? result.TotalHeight : null;
    }

    private static (float Top, float Bottom) ResolveVerticalBounds(BlockBox block)
    {
        var margin = BlockFlowMeasurement.ResolveMargin(block);
        var blockY = ResolvePublishedOrZeroY(block);
        var top = blockY - margin.Top;
        var bottom = blockY + ResolvePublishedOrZeroHeight(block) + margin.Bottom;
        return (top, bottom);
    }

    private static float ResolvePublishedOrZeroY(BlockBox block) =>
        block.UsedGeometry is { } geometry
            ? geometry.Y
            : 0f;

    private static float ResolvePublishedOrZeroHeight(BlockBox block) =>
        block.UsedGeometry is { } geometry
            ? geometry.Height
            : 0f;
}