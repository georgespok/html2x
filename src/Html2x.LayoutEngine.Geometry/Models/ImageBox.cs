using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine.Models;

internal sealed class ImageBox(BoxRole role) : BlockBox(role)
{
    public string Src { get; set; } = string.Empty;

    public SizePx AuthoredSizePx { get; set; }

    public SizePx IntrinsicSizePx { get; set; }

    public bool IsMissing { get; set; }

    public bool IsOversize { get; set; }

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
            IsMissing = IsMissing,
            IsOversize = IsOversize
        });
    }
}
