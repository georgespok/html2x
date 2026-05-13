using Html2x.LayoutEngine.Style;
using Html2x.RenderModel.Measurements.Units;

namespace Html2x.LayoutEngine;

/// <summary>
///     Input settings owned by the layout pipeline composition stage.
/// </summary>
internal sealed class LayoutBuildSettings
{
    public SizePt PageSize { get; init; } = PaperSizes.Letter;

    public StyleBuildSettings Style { get; init; } = new();
}
