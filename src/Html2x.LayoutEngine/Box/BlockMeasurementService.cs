using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

internal sealed class BlockMeasurementService
{
    public BlockMeasurementBasis Prepare(BlockBox box, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(box);

        var style = box.Style;
        var margin = style.Margin.Safe();
        var padding = style.Padding.Safe();
        var border = Spacing.FromBorderEdges(style.Borders).Safe();

        var width = style.WidthPt ?? Math.Max(0f, availableWidth - margin.Left - margin.Right);
        if (style.MinWidthPt.HasValue)
        {
            width = Math.Max(width, style.MinWidthPt.Value);
        }

        if (style.MaxWidthPt.HasValue)
        {
            width = Math.Min(width, style.MaxWidthPt.Value);
        }

        var contentWidth = Math.Max(0f, width - padding.Horizontal - border.Horizontal);
        if (box.MarkerOffset > 0f)
        {
            contentWidth = Math.Max(0f, contentWidth - box.MarkerOffset);
        }

        return new BlockMeasurementBasis(
            margin,
            padding,
            border,
            width,
            contentWidth);
    }

    public float ResolveContentHeight(
        BlockBox box,
        float resolvedContentHeight,
        float minimumContentHeight = 0f)
    {
        ArgumentNullException.ThrowIfNull(box);
        var contentHeight = Math.Max(minimumContentHeight, resolvedContentHeight);
        var style = box.Style;

        if (style.HeightPt.HasValue)
        {
            contentHeight = style.HeightPt.Value;
        }

        if (style.MinHeightPt.HasValue)
        {
            contentHeight = Math.Max(contentHeight, style.MinHeightPt.Value);
        }

        if (style.MaxHeightPt.HasValue)
        {
            contentHeight = Math.Min(contentHeight, style.MaxHeightPt.Value);
        }

        return Math.Max(0f, contentHeight);
    }

    public float MeasureStackedChildBlocks(
        IEnumerable<DisplayNode> children,
        float availableWidth,
        Func<BlockBox, float, float> measureBlockHeight,
        Func<TableBox, float, float> measureTableHeight)
    {
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(measureBlockHeight);
        ArgumentNullException.ThrowIfNull(measureTableHeight);

        var currentY = 0f;
        var previousBottomMargin = 0f;

        foreach (var child in children)
        {
            switch (child)
            {
                case TableBox table:
                {
                    var margin = table.Style.Margin.Safe();
                    var collapsedTop = Math.Max(previousBottomMargin, margin.Top);
                    currentY += collapsedTop + measureTableHeight(table, availableWidth);
                    previousBottomMargin = margin.Bottom;
                    break;
                }
                case BlockBox block:
                {
                    var margin = block.Style.Margin.Safe();
                    var collapsedTop = Math.Max(previousBottomMargin, margin.Top);
                    currentY += collapsedTop + measureBlockHeight(block, availableWidth);
                    previousBottomMargin = margin.Bottom;
                    break;
                }
            }
        }

        return Math.Max(0f, currentY + previousBottomMargin);
    }
}

internal readonly record struct BlockMeasurementBasis(
    Spacing Margin,
    Spacing Padding,
    Spacing Border,
    float ResolvedWidth,
    float ContentWidth);
