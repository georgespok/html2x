using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Applies resolved image metadata and block geometry to image boxes.
/// </summary>
internal sealed class ImageBlockLayoutApplier(IImageLayoutResolver imageResolver)
{
    private readonly IImageLayoutResolver _imageResolver =
        imageResolver ?? throw new ArgumentNullException(nameof(imageResolver));

    public void Apply(ImageBox node, BlockLayoutRequest request, BlockMeasurementBasis measurement)
    {
        ArgumentNullException.ThrowIfNull(node);

        var image = _imageResolver.Resolve(node, measurement.ContentFlowWidth);
        var origin = BlockPlacementService.ResolveOrigin(request, measurement.Margin);

        node.ApplyImageMetadata(
            image.Src,
            image.AuthoredSizePx,
            image.IntrinsicSizePx,
            image.Status,
            image.IsMissing,
            image.IsOversize);
        BlockLayoutState.Apply(
            node,
            measurement,
            BoxGeometryFactory.FromBorderBox(
                origin.X,
                origin.Y,
                image.TotalWidth,
                image.TotalHeight,
                measurement.Padding,
                measurement.Border,
                markerOffset: node.MarkerOffset));
    }
}
