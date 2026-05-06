using Html2x.RenderModel.Measurements.Units;

namespace Html2x.Options;

/// <summary>
///     Page-level conversion options.
/// </summary>
public sealed class PageOptions
{
    public SizePt Size { get; init; } = PaperSizes.Letter;
}