using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Formatting;

internal static class VerticalFlowPolicy
{
    public static float CollapseTopMargin(
        MarginCollapseRules marginCollapseRules,
        float previousBottomMargin,
        float nextTopMargin,
        FormattingContextKind contextKind,
        string consumerName,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(marginCollapseRules);

        return marginCollapseRules.Collapse(
            previousBottomMargin,
            nextTopMargin,
            contextKind,
            consumerName,
            diagnosticsSink);
    }

    public static float AdvanceCursorPast(float boxY, float boxHeight) => boxY + Math.Max(0f, boxHeight);

    public static float ResolveStackHeight(
        float currentY,
        float previousBottomMargin,
        float startY) =>
        Math.Max(0f, currentY + previousBottomMargin - startY);

    public static float ResolveContentHeight(float childBlockFlowHeight, float inlineFlowHeight) =>
        Math.Max(Math.Max(0f, childBlockFlowHeight), Math.Max(0f, inlineFlowHeight));
}