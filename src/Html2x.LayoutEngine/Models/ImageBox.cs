using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine.Models;

public sealed class ImageBox(DisplayRole role) : BlockBox(role)
{
    public string Src { get; set; } = string.Empty;

    public SizePx AuthoredSizePx { get; set; }

    public SizePx IntrinsicSizePx { get; set; }

    public bool IsMissing { get; set; }

    public bool IsOversize { get; set; }
}
