namespace Html2x.LayoutEngine.Geometry.Text;


/// <summary>
/// Carries the inputs needed to wrap inline runs into text lines.
/// </summary>
internal sealed record TextLayoutInput(
    IReadOnlyList<TextRunInput> Runs,
    float AvailableWidth,
    float LineHeight);
