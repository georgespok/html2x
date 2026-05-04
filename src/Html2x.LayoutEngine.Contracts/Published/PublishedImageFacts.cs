namespace Html2x.LayoutEngine.Contracts.Published;

using Html2x.RenderModel;

internal sealed record PublishedImageFacts
{
    public PublishedImageFacts(
        string src,
        SizePx authoredSizePx,
        SizePx intrinsicSizePx,
        bool isMissing,
        bool isOversize,
        ImageLoadStatus status = ImageLoadStatus.Ok)
    {
        ArgumentNullException.ThrowIfNull(src);

        Src = src;
        AuthoredSizePx = authoredSizePx;
        IntrinsicSizePx = intrinsicSizePx;
        IsMissing = isMissing;
        IsOversize = isOversize;
        Status = NormalizeStatus(status, isMissing, isOversize);
    }

    public string Src { get; }

    public SizePx AuthoredSizePx { get; }

    public SizePx IntrinsicSizePx { get; }

    public ImageLoadStatus Status { get; }

    public bool IsMissing { get; }

    public bool IsOversize { get; }

    private static ImageLoadStatus NormalizeStatus(ImageLoadStatus status, bool isMissing, bool isOversize)
    {
        if (status != ImageLoadStatus.Ok)
        {
            return status;
        }

        return isOversize
            ? ImageLoadStatus.Oversize
            : isMissing ? ImageLoadStatus.Missing : ImageLoadStatus.Ok;
    }
}
