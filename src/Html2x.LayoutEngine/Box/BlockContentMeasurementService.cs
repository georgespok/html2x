using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Measures block border-box heights without publishing layout geometry.
/// </summary>
internal sealed class BlockContentMeasurementService
{
    private readonly IInlineLayoutEngine _inlineEngine;
    private readonly BlockMeasurementService _measurement;
    private readonly IImageLayoutResolver _imageResolver;

    public BlockContentMeasurementService(
        IInlineLayoutEngine inlineEngine,
        BlockMeasurementService measurement,
        IImageLayoutResolver imageResolver)
    {
        _inlineEngine = inlineEngine ?? throw new ArgumentNullException(nameof(inlineEngine));
        _measurement = measurement ?? throw new ArgumentNullException(nameof(measurement));
        _imageResolver = imageResolver ?? throw new ArgumentNullException(nameof(imageResolver));
    }

    public float MeasureBorderBoxHeight(
        BlockBox block,
        float availableWidth,
        Func<TableBox, float, float> measureTableHeight)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(measureTableHeight);

        if (block is TableBox table)
        {
            return Math.Max(0f, measureTableHeight(table, availableWidth));
        }

        if (block is ImageBox imageBox)
        {
            var imageMeasurement = _measurement.Prepare(imageBox, availableWidth);
            return _imageResolver.Resolve(imageBox, imageMeasurement.ContentBoxWidth).TotalHeight;
        }

        if (block is RuleBox ruleBox)
        {
            var ruleMeasurement = _measurement.Prepare(ruleBox, availableWidth);
            return Math.Max(0f, ruleMeasurement.Padding.Vertical + ruleMeasurement.Border.Vertical);
        }

        var measurement = _measurement.Prepare(block, availableWidth);
        var inlineLayout = MeasureInlineLayout(block, InlineLayoutRequest.ForMeasurement(measurement.ContentBoxWidth));
        var nestedHeight = _measurement.MeasureStackedChildBlocks(
            block.Children,
            measurement.ContentBoxWidth,
            (child, childAvailableWidth) => MeasureBorderBoxHeight(child, childAvailableWidth, measureTableHeight),
            measureTableHeight);
        var contentHeight = _measurement.ResolveContentHeight(
            block,
            Math.Max(inlineLayout.TotalHeight, nestedHeight));

        return Math.Max(0f, contentHeight + measurement.Padding.Vertical + measurement.Border.Vertical);
    }

    private InlineLayoutResult MeasureInlineLayout(BlockBox block, InlineLayoutRequest request)
    {
        var previousLayout = block.InlineLayout;
        try
        {
            return _inlineEngine.Measure(block, request) ?? throw new InvalidOperationException(
                $"{nameof(IInlineLayoutEngine.Measure)} returned null for '{block.GetType().Name}'.");
        }
        finally
        {
            block.InlineLayout = previousLayout;
        }
    }
}
