using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine.Models;

public sealed class ImageBox(DisplayRole role) : BlockBox(role)
{
    public string Src { get; set; } = string.Empty;

    public SizePx AuthoredSizePx { get; set; }

    public SizePx IntrinsicSizePx { get; set; }

    public bool IsMissing { get; set; }

    public bool IsOversize { get; set; }

    protected override DisplayNode CloneShallowForParent(DisplayNode parent)
    {
        return new ImageBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Margin = Margin,
            Padding = Padding,
            TextAlign = TextAlign,
            MarkerOffset = MarkerOffset,
            UsedGeometry = UsedGeometry,
            IsAnonymous = IsAnonymous,
            IsInlineBlockContext = IsInlineBlockContext,
            Src = Src,
            AuthoredSizePx = AuthoredSizePx,
            IntrinsicSizePx = IntrinsicSizePx,
            IsMissing = IsMissing,
            IsOversize = IsOversize
        };
    }
}
