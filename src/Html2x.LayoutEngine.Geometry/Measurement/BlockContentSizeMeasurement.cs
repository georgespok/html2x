using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.LayoutEngine.Geometry.Primitives;

namespace Html2x.LayoutEngine.Geometry.Measurement;

/// <summary>
///     Measures content sizing facts without applying layout state to source boxes.
/// </summary>
/// <remarks>
///     Input is a source box tree plus available width. Output is measurement facts.
///     Measurement must not assign temporary geometry, inline layout, image metadata, or table metadata.
/// </remarks>
internal sealed class BlockContentSizeMeasurement(
    InlineFlowLayout inlineFlowLayout,
    BlockSizingRules sizingRules,
    ImageSizingRules imageSizingRules)
{
    private readonly ImageSizingRules _imageSizingRules =
        imageSizingRules ?? throw new ArgumentNullException(nameof(imageSizingRules));

    private readonly InlineFlowLayout _inlineFlowLayout =
        inlineFlowLayout ?? throw new ArgumentNullException(nameof(inlineFlowLayout));

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
                    var imageMeasurement = _sizingRules.ResolveBlockMeasurementBasis(imageBox, availableWidth);
                    var image = _imageSizingRules.ResolveImageLayout(imageBox, imageMeasurement.ContentFlowWidth);
                    return BlockContentSizeFacts.ForImage(image);
                }
            case RuleBox ruleBox:
                {
                    var ruleMeasurement = _sizingRules.ResolveBlockMeasurementBasis(ruleBox, availableWidth);
                    return new(
                        ruleMeasurement.Padding.Vertical + ruleMeasurement.Border.Vertical,
                        0f,
                        0f,
                        0f);
                }
        }

        var measurement = _sizingRules.ResolveBlockMeasurementBasis(block, availableWidth);
        var inlineContent = MeasureInlineContent(block, InlineLayoutRequest.ForMeasurement(measurement.ContentFlowWidth));
        var nestedHeight = _sizingRules.MeasureStackedChildBlocks(
            block.Children,
            measurement.ContentFlowWidth,
            (child, childAvailableWidth) => Measure(child, childAvailableWidth, measureTable).BorderBoxHeight,
            (tableChild, tableAvailableWidth) => measureTable(tableChild, tableAvailableWidth).BorderBoxHeight);
        var contentHeight = _sizingRules.ResolveContentHeight(
            block,
            Math.Max(inlineContent.TotalHeight, nestedHeight));
        var borderBoxHeight = UsedGeometryRules.ResolveBorderBoxHeight(
            contentHeight,
            measurement.Padding,
            measurement.Border);

        return new(
            borderBoxHeight,
            contentHeight,
            inlineContent.TotalHeight,
            nestedHeight);
    }

    private InlineContentSizeFacts MeasureInlineContent(BlockBox block, InlineLayoutRequest request) =>
        _inlineFlowLayout.MeasureInlineFlow(block, request);
}
