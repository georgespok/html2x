using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Formatting;

/// <summary>
///     Owns vertical margin collapse policy and diagnostics.
/// </summary>
internal sealed class MarginCollapseRules
{
    public float Collapse(
        float previousBottomMargin,
        float nextTopMargin,
        FormattingContextKind contextKind,
        string consumerName,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        var collapsedTopMargin = CollapseMarginPair(previousBottomMargin, nextTopMargin);

        diagnosticsSink?.Emit(new(
            GeometryDiagnosticNames.Stages.BoxTree,
            MarginCollapseDiagnosticNames.Event,
            DiagnosticSeverity.Info,
            null,
            null,
            DiagnosticFields.Create(
                DiagnosticFields.Field(MarginCollapseDiagnosticNames.Fields.PreviousBottomMargin, previousBottomMargin),
                DiagnosticFields.Field(MarginCollapseDiagnosticNames.Fields.NextTopMargin, nextTopMargin),
                DiagnosticFields.Field(MarginCollapseDiagnosticNames.Fields.CollapsedTopMargin, collapsedTopMargin),
                DiagnosticFields.Field(MarginCollapseDiagnosticNames.Fields.Owner,
                    GeometryDiagnosticNames.Owners.BlockFormattingContext),
                DiagnosticFields.Field(MarginCollapseDiagnosticNames.Fields.Consumer, consumerName),
                DiagnosticFields.Field(
                    GeometryDiagnosticNames.Fields.FormattingContext,
                    DiagnosticValue.FromEnum(contextKind))),
            DateTimeOffset.UtcNow));

        return collapsedTopMargin;
    }

    private static float CollapseMarginPair(float previousBottomMargin, float nextTopMargin)
    {
        if (previousBottomMargin >= 0f && nextTopMargin >= 0f)
        {
            return Math.Max(previousBottomMargin, nextTopMargin);
        }

        if (previousBottomMargin <= 0f && nextTopMargin <= 0f)
        {
            return Math.Min(previousBottomMargin, nextTopMargin);
        }

        return previousBottomMargin + nextTopMargin;
    }
}