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
