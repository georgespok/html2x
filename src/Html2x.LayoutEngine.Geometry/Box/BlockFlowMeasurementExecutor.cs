using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.LayoutEngine.Formatting;
using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Owns non-mutating block-flow measurement for stacked block children.
/// </summary>
internal sealed class BlockFlowMeasurementExecutor
{
    private readonly IBlockFormattingContext _blockFormattingContext;

    public BlockFlowMeasurementExecutor(IBlockFormattingContext blockFormattingContext)
    {
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
    }

    public bool TryMeasureStackedChildren(
        IEnumerable<BoxNode> children,
        float availableWidth,
        out float totalHeight,
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
            totalHeight = 0f;
            return false;
        }

        totalHeight = 0f;
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
                _blockFormattingContext,
                currentMarginBottom,
                nextMarginTop,
                formattingContext,
                consumerName,
                diagnosticsSink);
        }

        totalHeight += ResolveMargin(blockChildren[^1]).Bottom;
        totalHeight = Math.Max(0f, totalHeight);
        return true;
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

    private static bool HasAnySpacing(Spacing spacing)
    {
        return spacing.Left > 0f || spacing.Right > 0f || spacing.Top > 0f || spacing.Bottom > 0f;
    }
}
