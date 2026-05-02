namespace Html2x.LayoutEngine.Geometry.Published;

using Html2x.RenderModel;

internal sealed record PublishedImageFacts
{
    public PublishedImageFacts(
        string src,
        SizePx authoredSizePx,
        SizePx intrinsicSizePx,
        bool isMissing,
        bool isOversize)
    {
        ArgumentNullException.ThrowIfNull(src);

        Src = src;
        AuthoredSizePx = authoredSizePx;
        IntrinsicSizePx = intrinsicSizePx;
        IsMissing = isMissing;
        IsOversize = isOversize;
    }

    public string Src { get; }

    public SizePx AuthoredSizePx { get; }

    public SizePx IntrinsicSizePx { get; }

    public bool IsMissing { get; }

    public bool IsOversize { get; }
}
