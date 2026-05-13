using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Measurement;

/// <summary>
///     Measures block content metrics without mutating source boxes.
/// </summary>
internal sealed class BlockFormattingMetricsMeasurement
{
    private readonly BlockFlowMeasurement _flowMeasurement;

    public BlockFormattingMetricsMeasurement()
        : this(new())
    {
    }

    internal BlockFormattingMetricsMeasurement(MarginCollapseRules marginCollapseRules)
    {
        MarginCollapseRules = marginCollapseRules ?? throw new ArgumentNullException(nameof(marginCollapseRules));
        _flowMeasurement = new(MarginCollapseRules);
    }

    public MarginCollapseRules MarginCollapseRules { get; }

    public BlockFormattingMetricsResult Measure(BlockFormattingMetricsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blocks = new List<BlockBox>();
        CollectBlocksDepthFirst(request.RootBlock, blocks);

        var totalWidth = ResolveTotalWidth(request, blocks);
        var totalHeight = ResolveTotalHeight(request);
        float? baseline = request.FormattingContext == FormattingContextKind.InlineBlock ? 0f : null;

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

    private float ResolveTotalWidth(BlockFormattingMetricsRequest request, IReadOnlyList<BlockBox> blocks)
    {
        var directChildren = request.RootBlock.Children.OfType<BlockBox>().ToList();
        var maxWidth = 0f;

        if (directChildren.Count > 0)
        {
            foreach (var child in directChildren)
            {
                maxWidth = Math.Max(maxWidth, ResolveExplicitWidth(child));
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

                maxWidth = Math.Max(maxWidth, ResolveExplicitWidth(block));
            }
        }

        if (request.IsWidthUnbounded)
        {
            return maxWidth;
        }

        return Math.Min(request.AvailableWidth, maxWidth);
    }

    private static float ResolveExplicitWidth(BlockBox block) =>
        block.Style.WidthPt.HasValue
            ? Math.Max(0f, block.Style.WidthPt.Value)
            : 0f;

    private float ResolveTotalHeight(BlockFormattingMetricsRequest request)
    {
        var sequentialHeight = ResolveSequentialHeight(request);
        if (sequentialHeight.HasValue)
        {
            return sequentialHeight.Value;
        }

        return Math.Max(0f, BlockFlowMeasurement.ResolveBlockHeight(
            request.RootBlock,
            request.AvailableWidth,
            request.BlockHeightMeasurer,
            request.TableHeightMeasurer));
    }

    private float? ResolveSequentialHeight(BlockFormattingMetricsRequest request)
    {
        var result = _flowMeasurement.MeasureStackedChildren(
            request.RootBlock.Children,
            request.AvailableWidth,
            request.BlockHeightMeasurer,
            request.TableHeightMeasurer,
            request.ShouldEmitDiagnostics ? request.DiagnosticsSink : null,
            request.FormattingContext,
            request.DiagnosticConsumer);

        return result.HasBlocks ? result.TotalHeight : null;
    }

}
