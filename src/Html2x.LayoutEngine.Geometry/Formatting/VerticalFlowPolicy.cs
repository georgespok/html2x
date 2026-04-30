using Html2x.Abstractions.Layout.Fragments;
using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Formatting;

internal static class VerticalFlowPolicy
{
    public static float CollapseTopMargin(
        IBlockFormattingContext formattingContext,
        float previousBottomMargin,
        float nextTopMargin,
        FormattingContextKind contextKind,
        string consumerName,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(formattingContext);

        return formattingContext.CollapseMargins(
            previousBottomMargin,
            nextTopMargin,
            contextKind,
            consumerName,
            diagnosticsSink);
    }

    public static float AdvanceCursorPast(float boxY, float boxHeight)
    {
        return boxY + Math.Max(0f, boxHeight);
    }

    public static float ResolveStackHeight(
        float currentY,
        float previousBottomMargin,
        float startY)
    {
        return Math.Max(0f, currentY + previousBottomMargin - startY);
    }

    public static float ResolveContentHeight(float childBlockFlowHeight, float inlineFlowHeight)
    {
        return Math.Max(Math.Max(0f, childBlockFlowHeight), Math.Max(0f, inlineFlowHeight));
    }
}
