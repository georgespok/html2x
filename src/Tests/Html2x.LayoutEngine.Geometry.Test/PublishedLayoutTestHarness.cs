using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Geometry.Tables;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Test;

internal sealed class PublishedLayoutTestHarness
{
    public PublishedLayoutTestHarness(
        ITextMeasurer? textMeasurer = null,
        IDiagnosticsSink? diagnosticsSink = null,
        IImageMetadataResolver? imageMetadataResolver = null)
    {
        var metrics = new DefaultFontMetricsMeasurer();
        var resolvedTextMeasurer = textMeasurer ?? new FakeTextMeasurer(1f, 9f, 3f);
        ImageSizingRules = imageMetadataResolver is null
            ? new()
            : new(new()
            {
                ImageMetadataResolver = imageMetadataResolver
            });
        BlockFormattingMetrics = new();
        InlineFlowLayout = new(
            metrics,
            resolvedTextMeasurer,
            new LineHeightRules(),
            BlockFormattingMetrics,
            ImageSizingRules,
            diagnosticsSink);
        TableGridLayout = new(InlineFlowLayout, ImageSizingRules);
        BlockSizingRules = new(BlockFormattingMetrics.MarginCollapseRules);
        BlockLayout = new(
            InlineFlowLayout,
            TableGridLayout,
            BlockFormattingMetrics,
            ImageSizingRules,
            diagnosticsSink);
    }

    public BlockBoxLayout BlockLayout { get; }

    public BlockFormattingMetricsMeasurement BlockFormattingMetrics { get; }

    public BlockSizingRules BlockSizingRules { get; }

    public ImageSizingRules ImageSizingRules { get; }

    public InlineFlowLayout InlineFlowLayout { get; }

    public TableGridLayout TableGridLayout { get; }

    public PublishedLayoutTree Layout(BoxNode root, PageBox page) =>
        PublishedLayoutTestRunner.Run(BlockLayout, root, page);
}
