namespace Html2x.LayoutEngine.Geometry.Text;


/// <summary>
/// Represents one wrapped text line and its measured dimensions.
/// </summary>
internal sealed record TextLayoutLine(
    IReadOnlyList<TextLayoutRun> Runs,
    float LineWidth,
    float LineHeight);
