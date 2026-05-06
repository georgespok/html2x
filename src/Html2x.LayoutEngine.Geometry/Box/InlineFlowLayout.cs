using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.LayoutEngine.Geometry.Text;
using Html2x.RenderModel.Text;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Lays out inline content into line boxes and records inline layout on the owning block when requested.
/// </summary>
internal sealed class InlineFlowLayout
{
    private readonly InlineLayoutWriter _inlineLayoutWriter;
    private readonly ILineHeightStrategy _lineHeightStrategy;
    private readonly IFontMetricsProvider _metrics;
    private readonly InlineRunConstruction _runConstruction;
    private readonly LayoutBoxStateWriter _stateWriter;
    private readonly TextLineLayout _textLayout;
    private readonly ITextMeasurer _textMeasurer;

    public InlineFlowLayout()
        : this(
            new FontMetricsProvider(),
            null,
            new DefaultLineHeightStrategy(),
            new(),
            diagnosticsSink: null)
    {
    }

    public InlineFlowLayout(IFontMetricsProvider metrics)
        : this(
            metrics,
            null,
            new DefaultLineHeightStrategy(),
            new(),
            diagnosticsSink: null)
    {
    }

    public InlineFlowLayout(IFontMetricsProvider metrics, ITextMeasurer? textMeasurer,
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
        IFontMetricsProvider metrics,
        ITextMeasurer? textMeasurer,
        ILineHeightStrategy lineHeightStrategy,
        BlockContentExtentMeasurement contentMeasurement,
        IImageSizingRules? imageResolver = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _textMeasurer = textMeasurer ?? new FallbackTextMeasurer(_metrics);
        _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
        _runConstruction = new(
            _metrics,
            contentMeasurement ?? throw new ArgumentNullException(nameof(contentMeasurement)),
            imageResolver,
            diagnosticsSink);
        _textLayout = new(_textMeasurer);
        _inlineLayoutWriter = new(_textMeasurer);
        _stateWriter = new();
    }

    public InlineLayoutResult Layout(BlockBox block, InlineLayoutRequest request) => LayoutInlineFlow(block, request);

    public InlineLayoutResult Measure(BlockBox block, InlineLayoutRequest request) => MeasureInlineFlow(block, request);

    public InlineLayoutResult LayoutInlineFlow(BlockBox block, InlineLayoutRequest request)
    {
        var result = RunInlineFlow(block, request, LayoutSegment);
        _stateWriter.ApplyInlineLayout(block, result);
        return result;
    }

    public InlineLayoutResult MeasureInlineFlow(BlockBox block, InlineLayoutRequest request) =>
        RunInlineFlow(block, request, MeasureSegment);

    private InlineLayoutResult RunInlineFlow(
        BlockBox block,
        InlineLayoutRequest request,
        InlineSegmentFunction resolveSegment)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(resolveSegment);

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
                resolveSegment);

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
            resolveSegment);

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
        InlineSegmentFunction resolveSegment)
    {
        if (pendingInlineFlow.Count == 0)
        {
            return new(state, null);
        }

        var contentTop = state.CurrentY;

        var segment = resolveSegment(
            blockContext,
            pendingInlineFlow.Nodes,
            request.AvailableWidth,
            request.ContentLeft,
            contentTop,
            state.IncludeSyntheticListMarker);

        pendingInlineFlow.Clear();
        var nextState = state with
        {
            CurrentY = contentTop,
            IncludeSyntheticListMarker = false
        };

        if (segment is null)
        {
            return new(nextState, null);
        }

        nextState = nextState with
        {
            CurrentY = contentTop + segment.Value.Height,
            MaxLineWidth = Math.Max(state.MaxLineWidth, segment.Value.MaxLineWidth)
        };

        return new(nextState, segment.Value.Layout);
    }

    private InlineSegmentFlowResult? MeasureSegment(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        float availableWidth,
        float contentLeft,
        float contentTop,
        bool includeSyntheticListMarker)
    {
        var textLayout = MeasureTextLines(blockContext, inlineChildren, availableWidth, includeSyntheticListMarker);
        return textLayout is null
            ? null
            : new InlineSegmentFlowResult(null, textLayout.TotalHeight, textLayout.MaxLineWidth);
    }

    private InlineSegmentFlowResult? LayoutSegment(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        float availableWidth,
        float contentLeft,
        float contentTop,
        bool includeSyntheticListMarker)
    {
        var textLayout = MeasureTextLines(blockContext, inlineChildren, availableWidth, includeSyntheticListMarker);
        if (textLayout is null)
        {
            return null;
        }

        var layout = _inlineLayoutWriter.WriteSegment(
            blockContext,
            textLayout,
            contentLeft,
            contentTop,
            availableWidth,
            blockContext.TextAlign);

        return new InlineSegmentFlowResult(layout, textLayout.TotalHeight, textLayout.MaxLineWidth);
    }

    private TextLayoutResult? MeasureTextLines(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        float availableWidth,
        bool includeSyntheticListMarker)
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
        return _textLayout.Layout(new(runs, availableWidth, lineHeight));
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
            _runConstruction,
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

    private delegate InlineSegmentFlowResult? InlineSegmentFunction(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        float availableWidth,
        float contentLeft,
        float contentTop,
        bool includeSyntheticListMarker);

    private readonly record struct InlineSegmentFlowResult(
        InlineFlowSegmentLayout? Layout,
        float Height,
        float MaxLineWidth);

    private sealed class FallbackTextMeasurer(IFontMetricsProvider metricsProvider) : ITextMeasurer
    {
        private readonly IFontMetricsProvider _metricsProvider =
            metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));

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