using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Owns non-mutating block-flow measurement for stacked block children.
/// </summary>
internal sealed class BlockFlowMeasurement(MarginCollapseRules marginCollapseRules)
{
    private readonly MarginCollapseRules _marginCollapseRules =
        marginCollapseRules ?? throw new ArgumentNullException(nameof(marginCollapseRules));

    public BlockFlowMeasurementResult MeasureStackedChildren(
        IEnumerable<BoxNode> children,
        float availableWidth,
        Func<BlockBox, float, float>? blockHeightMeasurer = null,
        Func<TableBox, float, float>? tableHeightMeasurer = null,
        IDiagnosticsSink? diagnosticsSink = null,
        FormattingContextKind formattingContext = FormattingContextKind.Block,
        string consumerName = "unknown")
    {
        ArgumentNullException.ThrowIfNull(children);

        var blockChildren = children.OfType<BlockBox>().ToList();
        if (blockChildren.Count == 0)
        {
            return BlockFlowMeasurementResult.Empty;
        }

        var totalHeight = 0f;
        for (var i = 0; i < blockChildren.Count; i++)
        {
            var current = blockChildren[i];
            totalHeight += ResolveBlockHeight(
                current,
                availableWidth,
                blockHeightMeasurer,
                tableHeightMeasurer);

            if (i == blockChildren.Count - 1)
            {
                continue;
            }

            var currentMarginBottom = ResolveMargin(current).Bottom;
            var next = blockChildren[i + 1];
            var nextMarginTop = ResolveMargin(next).Top;
            totalHeight += VerticalFlowPolicy.CollapseTopMargin(
                _marginCollapseRules,
                currentMarginBottom,
                nextMarginTop,
                formattingContext,
                consumerName,
                diagnosticsSink);
        }

        totalHeight += ResolveMargin(blockChildren[^1]).Bottom;
        totalHeight = Math.Max(0f, totalHeight);
        return new(true, totalHeight);
    }

    internal static float ResolveBlockHeight(
        BlockBox block,
        float availableWidth,
        Func<BlockBox, float, float>? blockHeightMeasurer,
        Func<TableBox, float, float>? tableHeightMeasurer)
    {
        if (block is TableBox table && tableHeightMeasurer is not null)
        {
            return Math.Max(0f, tableHeightMeasurer(table, availableWidth));
        }

        if (blockHeightMeasurer is not null)
        {
            return Math.Max(0f, blockHeightMeasurer(block, availableWidth));
        }

        if (block.UsedGeometry is { } geometry)
        {
            return geometry.Height;
        }

        var padding = block.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(block.Style.Borders).Safe();

        if (block.Style.HeightPt is float explicitHeight)
        {
            return Math.Max(0f, explicitHeight) + padding.Vertical + border.Vertical;
        }

        return 0f;
    }

    internal static Spacing ResolveMargin(BlockBox block)
    {
        var styleMargin = block.Style.Margin.Safe();
        if (HasAnySpacing(styleMargin))
        {
            return styleMargin;
        }

        return block.Margin.Safe();
    }

    private static bool HasAnySpacing(Spacing spacing) =>
        spacing.Left > 0f || spacing.Right > 0f || spacing.Top > 0f || spacing.Bottom > 0f;
}

internal readonly record struct BlockFlowMeasurementResult(bool HasBlocks, float TotalHeight)
{
    public static BlockFlowMeasurementResult Empty { get; } = new(false, 0f);
}