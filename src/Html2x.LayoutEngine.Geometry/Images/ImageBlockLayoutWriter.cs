using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.LayoutEngine.Geometry.Writing;

namespace Html2x.LayoutEngine.Geometry.Images;

/// <summary>
///     Writes resolved image metadata and block geometry to image boxes.
/// </summary>
internal sealed class ImageBlockLayoutWriter(
    ImageSizingRules imageSizingRules,
    LayoutBoxStateWriter stateWriter)
{
    private readonly ImageSizingRules _imageSizingRules =
        imageSizingRules ?? throw new ArgumentNullException(nameof(imageSizingRules));

    private readonly LayoutBoxStateWriter _stateWriter =
        stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));

    public void Write(ImageBox node, BlockLayoutRequest request, BlockMeasurementBasis measurement)
    {
        ArgumentNullException.ThrowIfNull(node);

        var image = _imageSizingRules.ResolveImageLayout(node, measurement.ContentFlowWidth);
        var origin = BlockOriginRules.ResolveOrigin(request, measurement.Margin);

        node.ApplyImageMetadata(
            image.Src,
            image.AuthoredSizePx,
            image.IntrinsicSizePx,
            image.Status);
        _stateWriter.ApplyBlockLayout(
            node,
            measurement,
            UsedGeometryRules.FromBorderBox(
                origin.X,
                origin.Y,
                image.BorderBoxWidth,
                image.BorderBoxHeight,
                measurement.Padding,
                measurement.Border,
                markerOffset: node.MarkerOffset));
    }
}
