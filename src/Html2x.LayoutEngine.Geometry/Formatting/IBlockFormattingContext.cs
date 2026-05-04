using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Formatting;

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
