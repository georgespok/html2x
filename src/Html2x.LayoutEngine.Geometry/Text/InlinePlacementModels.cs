namespace Html2x.LayoutEngine.Text;

/// <summary>
/// Describes one text slice and the spacing applied while placing it on an inline line.
/// </summary>
internal readonly record struct TextRunPlacement(
    string Text,
    float Width,
    float LeftSpacing,
    float RightSpacing,
    float ExtraAfter);

/// <summary>
/// Carries the immutable line placement context shared by text and inline object placement.
/// </summary>
internal readonly record struct InlineLinePlacement(
    float ContentLeft,
    float TopY,
    float LineHeight,
    float BaselineY,
    float StartX);
