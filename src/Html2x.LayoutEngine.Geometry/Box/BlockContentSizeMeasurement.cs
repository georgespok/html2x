using Html2x.LayoutEngine.Geometry.Primitives;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Measures content sizing facts without applying layout state to source boxes.
/// </summary>
/// <remarks>
///     Input is a source box tree plus available width. Output is measurement facts.
///     Measurement must not assign temporary geometry, inline layout, image metadata, or table metadata.
/// </remarks>
internal sealed class BlockContentSizeMeasurement(
    InlineFlowLayout inlineEngine,
    BlockSizingRules sizingRules,
    IImageSizingRules imageResolver)
{
    private readonly IImageSizingRules _imageResolver =
        imageResolver ?? throw new ArgumentNullException(nameof(imageResolver));

    private readonly InlineFlowLayout _inlineEngine =
        inlineEngine ?? throw new ArgumentNullException(nameof(inlineEngine));

    private readonly BlockSizingRules
        _sizingRules = sizingRules ?? throw new ArgumentNullException(nameof(sizingRules));

    public BlockContentSizeFacts Measure(
        BlockBox block,
        float availableWidth,
        Func<TableBox, float, BlockContentSizeFacts> measureTable)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(measureTable);

        switch (block)
        {
            case TableBox table:
                return measureTable(table, availableWidth);
            case ImageBox imageBox:
            {
                var imageMeasurement = _sizingRules.Prepare(imageBox, availableWidth);
                var image = _imageResolver.Resolve(imageBox, imageMeasurement.ContentFlowWidth);
                return BlockContentSizeFacts.ForImage(image);
            }
            case RuleBox ruleBox:
            {
                var ruleMeasurement = _sizingRules.Prepare(ruleBox, availableWidth);
                return new(
                    ruleMeasurement.Padding.Vertical + ruleMeasurement.Border.Vertical,
                    0f,
                    0f,
                    0f);
            }
        }

        var measurement = _sizingRules.Prepare(block, availableWidth);
        var inlineLayout = MeasureInlineLayout(block, InlineLayoutRequest.ForMeasurement(measurement.ContentFlowWidth));
        var nestedHeight = _sizingRules.MeasureStackedChildBlocks(
            block.Children,
            measurement.ContentFlowWidth,
            (child, childAvailableWidth) => Measure(child, childAvailableWidth, measureTable).BorderBoxHeight,
            (tableChild, tableAvailableWidth) => measureTable(tableChild, tableAvailableWidth).BorderBoxHeight);
        var contentHeight = _sizingRules.ResolveContentHeight(
            block,
            Math.Max(inlineLayout.TotalHeight, nestedHeight));
        var borderBoxHeight = UsedGeometryRules.ResolveBorderBoxHeight(
            contentHeight,
            measurement.Padding,
            measurement.Border);

        return new(
            borderBoxHeight,
            contentHeight,
            inlineLayout.TotalHeight,
            nestedHeight);
    }

    private InlineLayoutResult MeasureInlineLayout(BlockBox block, InlineLayoutRequest request) =>
        _inlineEngine.MeasureInlineFlow(block, request) ?? throw new InvalidOperationException(
            $"{nameof(InlineFlowLayout.MeasureInlineFlow)} returned null for '{block.GetType().Name}'.");
}