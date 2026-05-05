using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.LayoutEngine.Geometry.Text;
using Html2x.RenderModel.Text;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Lays out inline content into line boxes and records inline layout on the owning block when requested.
/// </summary>
internal sealed class InlineLayoutEngine
{
    private readonly ILineHeightStrategy _lineHeightStrategy;
    private readonly IFontMetricsProvider _metrics;
    private readonly ITextMeasurer _textMeasurer;
    private readonly InlineRunFactory _runFactory;
    private readonly TextLayoutEngine _textLayout;
    private readonly InlineLayoutResultBuilder _layoutResultBuilder;
    private readonly LayoutBoxStateWriter _stateWriter;

    public InlineLayoutEngine()
        : this(
            new FontMetricsProvider(),
            null,
            new DefaultLineHeightStrategy(),
            new BlockFormattingContext(),
            diagnosticsSink: null)
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics)
        : this(
            metrics,
            null,
            new DefaultLineHeightStrategy(),
            new BlockFormattingContext(),
            diagnosticsSink: null)
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics, ITextMeasurer? textMeasurer, ILineHeightStrategy lineHeightStrategy)
        : this(
            metrics,
            textMeasurer,
            lineHeightStrategy,
            new BlockFormattingContext(),
            diagnosticsSink: null)
    {
    }

    internal InlineLayoutEngine(
        IFontMetricsProvider metrics,
        ITextMeasurer? textMeasurer,
        ILineHeightStrategy lineHeightStrategy,
        IBlockFormattingContext blockFormattingContext,
        IImageLayoutResolver? imageResolver = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _textMeasurer = textMeasurer ?? new FallbackTextMeasurer(_metrics);
        _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
        _runFactory = new InlineRunFactory(
            _metrics,
            blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext)),
            imageResolver,
            diagnosticsSink);
        _textLayout = new TextLayoutEngine(_textMeasurer);
        _layoutResultBuilder = new InlineLayoutResultBuilder(_textMeasurer);
        _stateWriter = new LayoutBoxStateWriter();
    }

    public InlineLayoutResult Layout(BlockBox block, InlineLayoutRequest request)
    {
        return LayoutInlineFlow(block, request);
    }

    public InlineLayoutResult Measure(BlockBox block, InlineLayoutRequest request)
    {
        return MeasureInlineFlow(block, request);
    }

    public InlineLayoutResult LayoutInlineFlow(BlockBox block, InlineLayoutRequest request)
    {
        var result = BuildLayout(block, request, includeSegments: true);
        _stateWriter.ApplyInlineLayout(block, result);
        return result;
    }

    public InlineLayoutResult MeasureInlineFlow(BlockBox block, InlineLayoutRequest request)
    {
        return BuildLayout(block, request, includeSegments: false);
    }

    private InlineLayoutResult BuildLayout(BlockBox block, InlineLayoutRequest request, bool includeSegments)
    {
        ArgumentNullException.ThrowIfNull(block);

        var segments = new List<InlineFlowSegmentLayout>();
        var pendingInlineFlow = new InlineFlowBuffer();
        var state = new InlineFlowState(
            request.ContentTop,
            request.IncludeSyntheticListMarker,
            0f);

        foreach (var child in block.Children)
        {
            if (pendingInlineFlow.TryQueue(child))
            {
                continue;
            }

            var flushResult = FlushPendingInlineFlow(
                block,
                request,
                pendingInlineFlow,
                state,
                includeSegments);

            state = flushResult.State;
            if (flushResult.Segment is not null)
            {
                segments.Add(flushResult.Segment);
            }

        }

        var finalFlushResult = FlushPendingInlineFlow(
            block,
            request,
            pendingInlineFlow,
            state,
            includeSegments);

        state = finalFlushResult.State;
        if (finalFlushResult.Segment is not null)
        {
            segments.Add(finalFlushResult.Segment);
        }

        var result = new InlineLayoutResult(
            segments,
            Math.Max(0f, state.CurrentY - request.ContentTop),
            state.MaxLineWidth);
        return result;
    }

    private InlineFlowFlushResult FlushPendingInlineFlow(
        BlockBox blockContext,
        InlineLayoutRequest request,
        InlineFlowBuffer pendingInlineFlow,
        InlineFlowState state,
        bool includeSegments)
    {
        if (pendingInlineFlow.Count == 0)
        {
            return new InlineFlowFlushResult(state, Segment: null);
        }

        var contentTop = state.CurrentY;

        var segment = BuildSegment(
            blockContext,
            pendingInlineFlow.Nodes,
            request.AvailableWidth,
            request.ContentLeft,
            contentTop,
            state.IncludeSyntheticListMarker,
            includeSegments);

        pendingInlineFlow.Clear();
        var nextState = state with
        {
            CurrentY = contentTop,
            IncludeSyntheticListMarker = false
        };

        if (segment is null)
        {
            return new InlineFlowFlushResult(nextState, Segment: null);
        }

        nextState = nextState with
        {
            CurrentY = contentTop + segment.Value.Height,
            MaxLineWidth = Math.Max(state.MaxLineWidth, segment.Value.MaxLineWidth)
        };

        return new InlineFlowFlushResult(nextState, segment.Value.Layout);
    }

    private InlineSegmentBuildResult? BuildSegment(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        float availableWidth,
        float contentLeft,
        float contentTop,
        bool includeSyntheticListMarker,
        bool includeSegments)
    {
        var runs = CollectInlineRuns(blockContext, inlineChildren, availableWidth, includeSyntheticListMarker);
        if (runs.Count == 0)
        {
            return null;
        }

        var font = _metrics.GetFontKey(blockContext.Style);
        var fontSize = _metrics.GetFontSize(blockContext.Style);
        var metrics = _textMeasurer.GetMetrics(font, fontSize);
        var lineHeight = _lineHeightStrategy.GetLineHeight(blockContext.Style, font, fontSize, metrics);
        var textLayout = _textLayout.Layout(new TextLayoutInput(runs, availableWidth, lineHeight));
        if (!includeSegments)
        {
            return new InlineSegmentBuildResult(null, textLayout.TotalHeight, textLayout.MaxLineWidth);
        }

        var layout = _layoutResultBuilder.BuildSegment(
            blockContext,
            textLayout,
            contentLeft,
            contentTop,
            availableWidth,
            blockContext.TextAlign);

        return new InlineSegmentBuildResult(layout, textLayout.TotalHeight, textLayout.MaxLineWidth);
    }

    private IReadOnlyList<TextRunInput> CollectInlineRuns(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        float availableWidth,
        bool includeSyntheticListMarker)
    {
        var collector = new InlineRunCollector(
            blockContext.Style,
            availableWidth,
            _runFactory,
            _textMeasurer,
            _lineHeightStrategy);

        if (includeSyntheticListMarker)
        {
            TryAppendSyntheticListMarkerRun(blockContext, collector);
        }

        var walker = new InlineRunTreeWalker(collector);
        walker.CollectInlineFlow(inlineChildren);

        return collector.Runs;
    }

    private void TryAppendSyntheticListMarkerRun(
        BlockBox blockContext,
        InlineRunCollector collector)
    {
        var marker = ListMarkerPolicy.CreateSyntheticMarker(blockContext);
        if (marker is not null)
        {
            collector.TryAppendTextRun(marker);
        }
    }

    private readonly record struct InlineFlowState(
        float CurrentY,
        bool IncludeSyntheticListMarker,
        float MaxLineWidth);

    private readonly record struct InlineFlowFlushResult(
        InlineFlowState State,
        InlineFlowSegmentLayout? Segment);

    private readonly record struct InlineSegmentBuildResult(
        InlineFlowSegmentLayout? Layout,
        float Height,
        float MaxLineWidth);

    private sealed class FallbackTextMeasurer(IFontMetricsProvider metricsProvider) : ITextMeasurer
    {
        private readonly IFontMetricsProvider _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));

        public TextMeasurement Measure(FontKey font, float sizePt, string text)
        {
            var width = MeasureWidth(font, sizePt, text);
            var (ascent, descent) = GetMetrics(font, sizePt);
            return TextMeasurement.CreateFallback(font, width, ascent, descent);
        }

        public float MeasureWidth(FontKey font, float sizePt, string text) =>
            _metricsProvider.MeasureTextWidth(font, sizePt, text);

        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt) =>
            _metricsProvider.GetMetrics(font, sizePt);
    }
}
