using Html2x.RenderModel;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.Text;

namespace Html2x.LayoutEngine.Text;


/// <summary>
/// Carries the inputs needed to wrap inline runs into text lines.
/// </summary>
internal sealed record TextLayoutInput(
    IReadOnlyList<TextRunInput> Runs,
    float AvailableWidth,
    float LineHeight);
