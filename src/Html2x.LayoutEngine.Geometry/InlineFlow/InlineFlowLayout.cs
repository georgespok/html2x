using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Geometry.Writing;
using Html2x.RenderModel.Text;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

/// <summary>
///     Lays out inline content into line boxes and records inline layout on the owning block when requested.
/// </summary>
internal sealed class InlineFlowLayout
{
    private readonly InlineFlowMeasurement _inlineFlowMeasurement;
    private readonly InlineLayoutWriter _inlineLayoutWriter;
    private readonly LayoutBoxStateWriter _stateWriter;

    public InlineFlowLayout()
        : this(
            new DefaultFontMetricsMeasurer(),
            null,
            new DefaultLineHeightStrategy(),
            new(),
            diagnosticsSink: null)
    {
    }

    public InlineFlowLayout(IFontMetricsMeasurer metrics)
        : this(
            metrics,
            null,
            new DefaultLineHeightStrategy(),
            new(),
            diagnosticsSink: null)
    {
    }

    public InlineFlowLayout(IFontMetricsMeasurer metrics, ITextMeasurer? textMeasurer,
        ILineHeightStrategy lineHeightStrategy)
        : this(
            metrics,
            textMeasurer,
            lineHeightStrategy,
            new(),
            diagnosticsSink: null)
    {
    }

    internal InlineFlowLayout(
        IFontMetricsMeasurer metrics,
        ITextMeasurer? textMeasurer,
        ILineHeightStrategy lineHeightStrategy,
        BlockFormattingMetricsMeasurement contentMeasurement,
        ImageSizingRules? imageSizingRules = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        var resolvedMetrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        ITextMeasurer resolvedTextMeasurer = textMeasurer is null
            ? new FallbackTextMeasurer(resolvedMetrics)
            : new ValidatedTextMeasurer(textMeasurer);
        var resolvedLineHeightStrategy =
            lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
        var runConstruction = new InlineRunConstruction(
            resolvedMetrics,
            contentMeasurement ?? throw new ArgumentNullException(nameof(contentMeasurement)),
            imageSizingRules,
            diagnosticsSink);
        _inlineFlowMeasurement = new(
            runConstruction,
            resolvedTextMeasurer,
            resolvedMetrics,
            resolvedLineHeightStrategy);
        _inlineLayoutWriter = new(resolvedTextMeasurer);
        _stateWriter = new();
    }

    public InlineLayoutResult LayoutInlineFlow(BlockBox block, InlineLayoutRequest request)
    {
        var measurement = _inlineFlowMeasurement.Measure(block, request);
        var segments = measurement.Segments.Select(WriteSegment).ToList();
        var result = new InlineLayoutResult(
            segments,
            measurement.TotalHeight,
            measurement.MaxLineWidth);
        _stateWriter.ApplyInlineLayout(block, result);
        return result;
    }

    public InlineLayoutResult LayoutInlineSegment(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        InlineLayoutRequest request)
    {
        var measurement = _inlineFlowMeasurement.MeasureSegment(
            blockContext,
            inlineChildren,
            request);
        if (measurement is null)
        {
            return InlineLayoutResult.Empty;
        }

        var segment = WriteSegment(measurement);
        return new(
            [segment],
            measurement.Height,
            measurement.MaxLineWidth);
    }

    public InlineContentSizeFacts MeasureInlineFlow(BlockBox block, InlineLayoutRequest request)
    {
        var measurement = _inlineFlowMeasurement.Measure(block, request);
        return new(
            measurement.TotalHeight,
            measurement.MaxLineWidth);
    }

    private InlineFlowSegmentLayout WriteSegment(InlineFlowSegmentMeasurement segment) =>
        _inlineLayoutWriter.WriteSegment(
            segment.BlockContext,
            segment.TextLayout,
            segment.ContentLeft,
            segment.ContentTop,
            segment.AvailableWidth,
            segment.BlockContext.TextAlign);

    private sealed class FallbackTextMeasurer(IFontMetricsMeasurer metricsMeasurer) : ITextMeasurer
    {
        private readonly IFontMetricsMeasurer _metricsMeasurer =
            metricsMeasurer ?? throw new ArgumentNullException(nameof(metricsMeasurer));

        public TextMeasurement Measure(FontKey font, float sizePt, string text)
        {
            var width = MeasureWidth(font, sizePt, text);
            var (ascent, descent) = GetMetrics(font, sizePt);
            return TextMeasurement.CreateFallback(font, width, ascent, descent);
        }

        private float MeasureWidth(FontKey font, float sizePt, string text) =>
            _metricsMeasurer.MeasureTextWidth(font, sizePt, text);

        private (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt) =>
            _metricsMeasurer.GetMetrics(font, sizePt);
    }
}
