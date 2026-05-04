using System.Text;
using Html2x.RenderModel;
using SkiaSharp;

namespace Html2x.Resources;


internal sealed class ImageResourceResult
{
    public required string Src { get; init; }

    public ImageResourceStatus Status { get; init; }

    public byte[]? Bytes { get; init; }

    public SizePx IntrinsicSizePx { get; init; }
}
