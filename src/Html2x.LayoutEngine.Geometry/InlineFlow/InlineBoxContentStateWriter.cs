using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.Writing;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

/// <summary>
///     Writes placed inline-block content geometry, nested inline layout, and image metadata.
/// </summary>
internal sealed class InlineBoxContentStateWriter(LayoutBoxStateWriter stateWriter)
{
    private readonly LayoutBoxStateWriter _stateWriter =
        stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));

    public InlineBoxContentStateWriter()
        : this(new())
    {
    }

    public void ApplyInlineBoxContentLayout(
        BlockBox contentBox,
        Spacing margin,
        Spacing padding,
        UsedGeometry geometry,
        InlineLayoutResult inlineLayout,
        ImageLayoutResolution? imageResolution = null)
    {
        ArgumentNullException.ThrowIfNull(contentBox);
        ArgumentNullException.ThrowIfNull(inlineLayout);

        _stateWriter.ApplyBlockLayout(contentBox, margin, padding, geometry);
        _stateWriter.ApplyInlineLayout(contentBox, inlineLayout);

        if (contentBox is ImageBox imageBox && imageResolution is { } resolvedImage)
        {
            imageBox.ApplyImageMetadata(
                resolvedImage.Src,
                resolvedImage.AuthoredSizePx,
                resolvedImage.IntrinsicSizePx,
                resolvedImage.Status);
        }
    }
}
