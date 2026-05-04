using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Geometry.Models;

internal sealed class ImageBox(BoxRole role) : BlockBox(role)
{
    public string Src { get; internal set; } = string.Empty;

    public SizePx AuthoredSizePx { get; internal set; }

    public SizePx IntrinsicSizePx { get; internal set; }

    public ImageLoadStatus Status { get; internal set; }

    public bool IsMissing { get; internal set; }

    public bool IsOversize { get; internal set; }

    internal void ApplyImageMetadata(
        string src,
        SizePx authoredSizePx,
        SizePx intrinsicSizePx,
        ImageLoadStatus status,
        bool isMissing,
        bool isOversize)
    {
        Src = src;
        AuthoredSizePx = authoredSizePx;
        IntrinsicSizePx = intrinsicSizePx;
        Status = status;
        IsMissing = isMissing;
        IsOversize = isOversize;
    }

    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return CopyBlockStateTo(new ImageBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
            SourceIdentity = SourceIdentity,
            Src = Src,
            AuthoredSizePx = AuthoredSizePx,
            IntrinsicSizePx = IntrinsicSizePx,
            Status = Status,
            IsMissing = IsMissing,
            IsOversize = IsOversize
        });
    }
}
