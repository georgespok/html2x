using Html2x.LayoutEngine.Style;
using Html2x.RenderModel.Measurements.Units;

namespace Html2x.LayoutEngine;

/// <summary>
/// Input settings owned by the layout pipeline composition stage.
/// </summary>
internal sealed class LayoutBuildSettings
{
    public SizePt PageSize { get; init; } = PaperSizes.Letter;

    /// <summary>Base directory used to resolve relative image paths during layout.</summary>
    public string? ResourceBaseDirectory { get; init; }

    /// <summary>Maximum allowed image size in bytes; images over this are marked oversize.</summary>
    public long MaxImageSizeBytes { get; init; } = 10 * 1024 * 1024;

    public StyleBuildSettings Style { get; init; } = new();
}
