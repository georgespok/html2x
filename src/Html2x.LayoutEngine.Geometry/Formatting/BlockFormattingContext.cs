using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Formatting;

/// <summary>
/// Owns supported block formatting measurement and margin-collapse policy.
/// </summary>
internal sealed class BlockFormattingContext : IBlockFormattingContext
{
    private readonly BlockFlowMeasurementExecutor _flowMeasurement;

    public BlockFormattingContext()
    {
        _flowMeasurement = new BlockFlowMeasurementExecutor(this);
    }

    public BlockFormattingResult Format(BlockFormattingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blocks = new List<BlockBox>();
        CollectBlocksDepthFirst(request.RootBlock, blocks);

        var totalWidth = ResolveTotalWidth(request, blocks);
        var totalHeight = ResolveTotalHeight(request, blocks);
        float? baseline = request.ContextKind == FormattingContextKind.InlineBlock
            ? ResolvePublishedOrZeroHeight(request.RootBlock)
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

        var borderBoxWidth = BoxDimensionResolver.ResolveBlockBorderBoxWidth(
            style,
            availableWidth,
            margin,
            padding,
            border);
        var contentFlowWidth = BoxDimensionResolver.ResolveContentFlowWidth(borderBoxWidth, padding, border, box.MarkerOffset);

        return new BlockMeasurementBasis(
            margin,
            padding,
            border,
            borderBoxWidth,
            contentFlowWidth);
    }

    public float CollapseMargins(
        float previousBottomMargin,
        float nextTopMargin,
        FormattingContextKind contextKind,
        string consumerName,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        var collapsedTopMargin = CollapseMarginPair(previousBottomMargin, nextTopMargin);

        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: GeometryDiagnosticNames.Stages.BoxTree,
            Name: MarginCollapseDiagnosticNames.Event,
            Severity: DiagnosticSeverity.Info,
            Message: null,
            Context: null,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field(MarginCollapseDiagnosticNames.Fields.PreviousBottomMargin, previousBottomMargin),
                DiagnosticFields.Field(MarginCollapseDiagnosticNames.Fields.NextTopMargin, nextTopMargin),
                DiagnosticFields.Field(MarginCollapseDiagnosticNames.Fields.CollapsedTopMargin, collapsedTopMargin),
                DiagnosticFields.Field(MarginCollapseDiagnosticNames.Fields.Owner, nameof(BlockFormattingContext)),
                DiagnosticFields.Field(MarginCollapseDiagnosticNames.Fields.Consumer, consumerName),
                DiagnosticFields.Field(
                    GeometryDiagnosticNames.Fields.FormattingContext,
                    DiagnosticValue.FromEnum(contextKind))),
            Timestamp: DateTimeOffset.UtcNow));

        return collapsedTopMargin;
    }

    private static float CollapseMarginPair(float previousBottomMargin, float nextTopMargin)
    {
        if (previousBottomMargin >= 0f && nextTopMargin >= 0f)
        {
            return Math.Max(previousBottomMargin, nextTopMargin);
        }

        if (previousBottomMargin <= 0f && nextTopMargin <= 0f)
        {
            return Math.Min(previousBottomMargin, nextTopMargin);
        }

        return previousBottomMargin + nextTopMargin;
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

    private float ResolveTotalHeight(BlockFormattingRequest request, IReadOnlyList<BlockBox> blocks)
    {
        var sequentialHeight = ResolveSequentialHeight(request);
        if (sequentialHeight.HasValue)
        {
            return Math.Max(Math.Max(0f, ResolvePublishedOrZeroHeight(request.RootBlock)), sequentialHeight.Value);
        }

        if (blocks.Count == 0)
        {
            return Math.Max(0f, BlockFlowMeasurementExecutor.ResolveBlockHeight(
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

    private float? ResolveSequentialHeight(BlockFormattingRequest request)
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
        var margin = BlockFlowMeasurementExecutor.ResolveMargin(block);
        var blockY = ResolvePublishedOrZeroY(block);
        var top = blockY - margin.Top;
        var bottom = blockY + ResolvePublishedOrZeroHeight(block) + margin.Bottom;
        return (top, bottom);
    }

    private static float ResolvePublishedOrZeroY(BlockBox block)
    {
        return block.UsedGeometry is { } geometry
            ? geometry.Y
            : 0f;
    }

    private static float ResolvePublishedOrZeroHeight(BlockBox block)
    {
        return block.UsedGeometry is { } geometry
            ? geometry.Height
            : 0f;
    }
}
