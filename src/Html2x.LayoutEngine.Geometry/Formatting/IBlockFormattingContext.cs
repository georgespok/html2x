using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Formatting;

internal interface IBlockFormattingContext
{
    BlockFormattingResult Format(BlockFormattingRequest request);
    BlockMeasurementBasis Measure(BlockBox box, float availableWidth);
    float CollapseMargins(
        float previousBottomMargin,
        float nextTopMargin,
        FormattingContextKind contextKind,
        string consumerName,
        IDiagnosticsSink? diagnosticsSink = null);
}
