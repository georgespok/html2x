using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;

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
        DiagnosticsSession? diagnosticsSession = null);
}
