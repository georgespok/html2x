using Html2x.LayoutEngine.Geometry.Primitives;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Applies resolved image metadata and block geometry to image boxes.
/// </summary>
internal sealed class ImageBlockLayoutApplier(
    IImageLayoutResolver imageResolver,
    LayoutBoxStateWriter stateWriter)
{
    private readonly IImageLayoutResolver _imageResolver =
        imageResolver ?? throw new ArgumentNullException(nameof(imageResolver));
    private readonly LayoutBoxStateWriter _stateWriter =
        stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));

    public void Apply(ImageBox node, BlockLayoutRequest request, BlockMeasurementBasis measurement)
    {
        ArgumentNullException.ThrowIfNull(node);

        var image = _imageResolver.Resolve(node, measurement.ContentFlowWidth);
        var origin = BlockOriginResolver.ResolveOrigin(request, measurement.Margin);

        _stateWriter.ApplyImageBlockLayout(
            node,
            measurement,
            UsedGeometryCalculator.FromBorderBox(
                origin.X,
                origin.Y,
                image.TotalWidth,
                image.TotalHeight,
                measurement.Padding,
                measurement.Border,
                markerOffset: node.MarkerOffset),
            image);
    }
}
