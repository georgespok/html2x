using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.RenderModel.Documents;

/// <summary>
/// Renderer-facing page facts in absolute page coordinates.
/// </summary>
/// <param name="Size">Full page size in points.</param>
/// <param name="Margins">Content margins in points.</param>
/// <param name="Children">Fragments positioned in absolute page coordinates.</param>
/// <param name="PageNumber">One-based page number.</param>
/// <param name="PageBackground">Optional page background color. A null value means white.</param>
public sealed record LayoutPage(
    SizePt Size,
    Spacing Margins,
    IReadOnlyList<Fragment> Children,
    int PageNumber = 1,
    ColorRgba? PageBackground = null
)
{
    /// <summary>
    /// Gets fragments positioned in absolute page coordinates.
    /// </summary>
    public IReadOnlyList<Fragment> Children { get; init; } =
        Children?.ToArray() ?? throw new ArgumentNullException(nameof(Children));

    /// <summary>
    /// Gets the full page size in points.
    /// </summary>
    public SizePt PageSize => Size;
}
