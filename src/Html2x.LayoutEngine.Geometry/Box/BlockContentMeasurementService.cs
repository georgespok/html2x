using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;


/// <summary>
/// Measures content sizing facts without applying layout state to source boxes.
/// </summary>
/// <remarks>
/// Input is a source box tree plus available width. Output is measurement facts.
/// Measurement must not assign temporary geometry, inline layout, image metadata, or table metadata.
/// </remarks>
internal sealed class BlockContentMeasurementService
{
    private readonly InlineLayoutEngine _inlineEngine;
    private readonly BlockMeasurementService _measurement;
    private readonly IImageLayoutResolver _imageResolver;

    public BlockContentMeasurementService(
        InlineLayoutEngine inlineEngine,
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
        ArgumentNullException.ThrowIfNull(measureTableHeight);

        return Measure(
            block,
            availableWidth,
            (table, tableAvailableWidth) => BlockContentMeasurement.ForBorderBoxHeight(
                measureTableHeight(table, tableAvailableWidth)))
            .BorderBoxHeight;
    }

    public BlockContentMeasurement Measure(
        BlockBox block,
        float availableWidth,
        Func<TableBox, float, BlockContentMeasurement> measureTable)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(measureTable);

        if (block is TableBox table)
        {
            return measureTable(table, availableWidth);
        }

        if (block is ImageBox imageBox)
        {
            var imageMeasurement = _measurement.Prepare(imageBox, availableWidth);
            var image = _imageResolver.Resolve(imageBox, imageMeasurement.ContentFlowWidth);
            return BlockContentMeasurement.ForImage(image);
        }

        if (block is RuleBox ruleBox)
        {
            var ruleMeasurement = _measurement.Prepare(ruleBox, availableWidth);
            return new BlockContentMeasurement(
                ruleMeasurement.Padding.Vertical + ruleMeasurement.Border.Vertical,
                contentHeight: 0f,
                inlineHeight: 0f,
                nestedBlockHeight: 0f);
        }

        var measurement = _measurement.Prepare(block, availableWidth);
        var inlineLayout = MeasureInlineLayout(block, InlineLayoutRequest.ForMeasurement(measurement.ContentFlowWidth));
        var nestedHeight = _measurement.MeasureStackedChildBlocks(
            block.Children,
            measurement.ContentFlowWidth,
            (child, childAvailableWidth) => Measure(child, childAvailableWidth, measureTable).BorderBoxHeight,
            (tableChild, tableAvailableWidth) => measureTable(tableChild, tableAvailableWidth).BorderBoxHeight);
        var contentHeight = _measurement.ResolveContentHeight(
            block,
            Math.Max(inlineLayout.TotalHeight, nestedHeight));
        var borderBoxHeight = BoxGeometryFactory.ResolveBorderBoxHeight(
            contentHeight,
            measurement.Padding,
            measurement.Border);

        return new BlockContentMeasurement(
            borderBoxHeight,
            contentHeight,
            inlineLayout.TotalHeight,
            nestedHeight);
    }

    private InlineLayoutResult MeasureInlineLayout(BlockBox block, InlineLayoutRequest request)
    {
        return _inlineEngine.Measure(block, request) ?? throw new InvalidOperationException(
            $"{nameof(InlineLayoutEngine.Measure)} returned null for '{block.GetType().Name}'.");
    }
}
