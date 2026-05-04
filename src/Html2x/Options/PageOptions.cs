using Html2x.RenderModel;

namespace Html2x;


/// <summary>
/// Page-level conversion options.
/// </summary>
public sealed class PageOptions
{
    public SizePt Size { get; init; } = PaperSizes.Letter;
}
