using Html2x.RenderModel;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.Text;

namespace Html2x.LayoutEngine.Text;


/// <summary>
/// Represents one wrapped text line and its measured dimensions.
/// </summary>
internal sealed record TextLayoutLine(
    IReadOnlyList<TextLayoutRun> Runs,
    float LineWidth,
    float LineHeight);
