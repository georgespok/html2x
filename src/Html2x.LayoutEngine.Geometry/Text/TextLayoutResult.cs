using Html2x.RenderModel;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.Text;

namespace Html2x.LayoutEngine.Text;


/// <summary>
/// Carries all wrapped lines and aggregate inline layout metrics.
/// </summary>
internal sealed record TextLayoutResult(
    IReadOnlyList<TextLayoutLine> Lines,
    float TotalHeight,
    float MaxLineWidth);
