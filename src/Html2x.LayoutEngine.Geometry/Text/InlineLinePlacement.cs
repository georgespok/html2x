namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Carries the immutable line placement context shared by text and inline object placement.
/// </summary>
internal readonly record struct InlineLinePlacement(
    float ContentLeft,
    float TopY,
    float LineHeight,
    float BaselineY,
    float StartX);