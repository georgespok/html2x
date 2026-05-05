using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;

namespace Html2x.LayoutEngine.Geometry.Models;

internal sealed class ImageBox(BoxRole role) : BlockBox(role)
{
    public string Src { get; internal set; } = string.Empty;

    public SizePx AuthoredSizePx { get; internal set; }

    public SizePx IntrinsicSizePx { get; internal set; }

    public ImageLoadStatus Status { get; internal set; }

    public bool IsMissing => ImageLoadStatusFacts.IsMissing(Status);

    public bool IsOversize => ImageLoadStatusFacts.IsOversize(Status);

    internal void ApplyImageMetadata(
        string src,
        SizePx authoredSizePx,
        SizePx intrinsicSizePx,
        ImageLoadStatus status)
    {
        Src = src;
        AuthoredSizePx = authoredSizePx;
        IntrinsicSizePx = intrinsicSizePx;
        Status = status;
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
            Status = Status
        });
    }
}
