using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;

namespace Html2x.LayoutEngine.Contracts.Published;

internal sealed record PublishedImageFacts
{
    public PublishedImageFacts(
        string src,
        SizePx authoredSizePx,
        SizePx intrinsicSizePx,
        ImageLoadStatus status = ImageLoadStatus.Ok)
    {
        ArgumentNullException.ThrowIfNull(src);

        Src = src;
        AuthoredSizePx = authoredSizePx;
        IntrinsicSizePx = intrinsicSizePx;
        Status = status;
    }

    public string Src { get; }

    public SizePx AuthoredSizePx { get; }

    public SizePx IntrinsicSizePx { get; }

    public ImageLoadStatus Status { get; }

    public bool IsMissing => ImageLoadStatusFacts.IsMissing(Status);

    public bool IsOversize => ImageLoadStatusFacts.IsOversize(Status);
}