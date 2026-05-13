using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Measurement;

/// <summary>
///     Measures inline-flow line facts without writing inline layout state.
/// </summary>
internal sealed class InlineFlowMeasurement(
    InlineRunConstruction runConstruction,
    ITextMeasurer textMeasurer,
    IFontMetricsMeasurer metrics,
    ILineHeightStrategy lineHeightStrategy)
{
    private readonly ILineHeightStrategy _lineHeightStrategy =
        lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));

    private readonly IFontMetricsMeasurer _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

    private readonly InlineRunConstruction _runConstruction =
        runConstruction ?? throw new ArgumentNullException(nameof(runConstruction));

    private readonly TextLineLayout _textLayout = new(textMeasurer);

    private readonly ITextMeasurer _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));

    public InlineFlowMeasurementResult Measure(BlockBox block, InlineLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(request);

        var segments = new List<InlineFlowSegmentMeasurement>();
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
                state);

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
            state);

        state = finalFlushResult.State;
        if (finalFlushResult.Segment is not null)
        {
            segments.Add(finalFlushResult.Segment);
        }

        return new(
            segments,
            Math.Max(0f, state.CurrentY - request.ContentTop),
            state.MaxLineWidth);
    }

    private InlineFlowFlushResult FlushPendingInlineFlow(
        BlockBox blockContext,
        InlineLayoutRequest request,
        InlineFlowBuffer pendingInlineFlow,
        InlineFlowState state)
    {
        if (pendingInlineFlow.Count == 0)
        {
            return new(state, null);
        }

        var contentTop = state.CurrentY;

        var segment = MeasureSegmentCore(
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
            CurrentY = contentTop + segment.Height,
            MaxLineWidth = Math.Max(state.MaxLineWidth, segment.MaxLineWidth)
        };

        return new(nextState, segment);
    }

    public InlineFlowSegmentMeasurement? MeasureSegment(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        InlineLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(blockContext);
        ArgumentNullException.ThrowIfNull(inlineChildren);

        return MeasureSegmentCore(
            blockContext,
            inlineChildren,
            request.AvailableWidth,
            request.ContentLeft,
            request.ContentTop,
            request.IncludeSyntheticListMarker);
    }

    private InlineFlowSegmentMeasurement? MeasureSegmentCore(
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
            : new(
                blockContext,
                textLayout,
                contentLeft,
                contentTop,
                availableWidth);
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
        var fontMeasurement = _textMeasurer.Measure(font, fontSize, string.Empty);
        var metrics = (fontMeasurement.Ascent, fontMeasurement.Descent);
        var lineHeight = _lineHeightStrategy.GetLineHeight(blockContext.Style, font, fontSize, metrics);
        return _textLayout.Layout(new(runs, availableWidth, lineHeight));
    }

    private IReadOnlyList<TextRunInput> CollectInlineRuns(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        float availableWidth,
        bool includeSyntheticListMarker)
    {
        var collection = new InlineRunCollector(
            _runConstruction,
            _textMeasurer,
            _lineHeightStrategy);
        return collection.CollectInlineFlow(
            blockContext,
            inlineChildren,
            availableWidth,
            includeSyntheticListMarker);
    }

    private readonly record struct InlineFlowState(
        float CurrentY,
        bool IncludeSyntheticListMarker,
        float MaxLineWidth);

    private readonly record struct InlineFlowFlushResult(
        InlineFlowState State,
        InlineFlowSegmentMeasurement? Segment);
}

internal sealed record InlineFlowMeasurementResult(
    IReadOnlyList<InlineFlowSegmentMeasurement> Segments,
    float TotalHeight,
    float MaxLineWidth);

internal sealed record InlineFlowSegmentMeasurement(
    BlockBox BlockContext,
    TextLayoutResult TextLayout,
    float ContentLeft,
    float ContentTop,
    float AvailableWidth)
{
    public float Height => TextLayout.TotalHeight;

    public float MaxLineWidth => TextLayout.MaxLineWidth;
}
