using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Text;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Lays out inline content into line boxes and records inline layout on the owning block when requested.
/// </summary>
public sealed class InlineLayoutEngine : IInlineLayoutEngine, IInlineFormattingContextRunner
{
    private readonly ILineHeightStrategy _lineHeightStrategy;
    private readonly IFontMetricsProvider _metrics;
    private readonly ITextMeasurer _textMeasurer;
    private readonly InlineRunFactory _runFactory;
    private readonly TextLayoutEngine _textLayout;
    private readonly InlineLayoutResultBuilder _layoutResultBuilder;
    private readonly InlineNodeMeasurerRegistry _nodeMeasurers;
    private readonly IBlockFormattingContext _blockFormattingContext;
    private readonly DiagnosticsSession? _diagnosticsSession;

    public InlineLayoutEngine()
        : this(
            new FontMetricsProvider(),
            null,
            new DefaultLineHeightStrategy(),
            new BlockFormattingContext(),
            InlineNodeMeasurerRegistry.CreateDefault(),
            diagnosticsSession: null)
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics)
        : this(
            metrics,
            null,
            new DefaultLineHeightStrategy(),
            new BlockFormattingContext(),
            InlineNodeMeasurerRegistry.CreateDefault(),
            diagnosticsSession: null)
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics, ITextMeasurer? textMeasurer, ILineHeightStrategy lineHeightStrategy)
        : this(
            metrics,
            textMeasurer,
            lineHeightStrategy,
            new BlockFormattingContext(),
            InlineNodeMeasurerRegistry.CreateDefault(),
            diagnosticsSession: null)
    {
    }

    internal InlineLayoutEngine(
        IFontMetricsProvider metrics,
        ITextMeasurer? textMeasurer,
        ILineHeightStrategy lineHeightStrategy,
        IBlockFormattingContext blockFormattingContext,
        InlineNodeMeasurerRegistry nodeMeasurers,
        IImageLayoutResolver? imageResolver = null,
        DiagnosticsSession? diagnosticsSession = null)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _textMeasurer = textMeasurer ?? new FallbackTextMeasurer(_metrics);
        _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _diagnosticsSession = diagnosticsSession;
        _runFactory = new InlineRunFactory(_metrics, _blockFormattingContext, imageResolver, _diagnosticsSession);
        _textLayout = new TextLayoutEngine(_textMeasurer);
        _layoutResultBuilder = new InlineLayoutResultBuilder(_textMeasurer);
        _nodeMeasurers = nodeMeasurers ?? throw new ArgumentNullException(nameof(nodeMeasurers));
    }

    public InlineLayoutResult Layout(BlockBox block, InlineLayoutRequest request)
    {
        var result = BuildLayout(block, request, includeSegments: true);
        block.InlineLayout = result;
        return result;
    }

    public InlineLayoutResult Measure(BlockBox block, InlineLayoutRequest request)
    {
        return BuildLayout(block, request, includeSegments: false);
    }

    public InlineLayoutResult LayoutInlineContent(BlockBox block, InlineLayoutRequest request)
    {
        return Layout(block, request);
    }

    public InlineLayoutResult MeasureInlineContent(BlockBox block, InlineLayoutRequest request)
    {
        return Measure(block, request);
    }

    private InlineLayoutResult BuildLayout(BlockBox block, InlineLayoutRequest request, bool includeSegments)
    {
        ArgumentNullException.ThrowIfNull(block);

        var segments = new List<InlineFlowSegmentLayout>();
        var pendingInlineFlow = new List<DisplayNode>();
        var state = new InlineFlowState(
            request.ContentTop,
            0f,
            request.IncludeSyntheticListMarker,
            0f);

        foreach (var child in block.Children)
        {
            if (TryAppendInlineFlowNode(child, pendingInlineFlow))
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

            if (child is BlockBox childBlock)
            {
                state = AdvancePastBlockChild(block, childBlock, state);
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
            Math.Max(0f, state.CurrentY + state.PreviousBottomMargin - request.ContentTop),
            state.MaxLineWidth);
        return result;
    }

    private InlineFlowFlushResult FlushPendingInlineFlow(
        BlockBox blockContext,
        InlineLayoutRequest request,
        List<DisplayNode> pendingInlineFlow,
        InlineFlowState state,
        bool includeSegments)
    {
        if (pendingInlineFlow.Count == 0)
        {
            return new InlineFlowFlushResult(state, Segment: null);
        }

        var contentTop = state.CurrentY + state.PreviousBottomMargin;

        var segment = BuildSegment(
            blockContext,
            pendingInlineFlow,
            request.AvailableWidth,
            request.ContentLeft,
            contentTop,
            state.IncludeSyntheticListMarker,
            includeSegments);

        pendingInlineFlow.Clear();
        var nextState = state with
        {
            CurrentY = contentTop,
            PreviousBottomMargin = 0f,
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

    private InlineFlowState AdvancePastBlockChild(
        BlockBox blockContext,
        BlockBox childBlock,
        InlineFlowState state)
    {
        var margin = childBlock.Style.Margin.Safe();
        var collapsedTop = _blockFormattingContext.CollapseMargins(
            state.PreviousBottomMargin,
            margin.Top,
            ResolveFormattingContext(blockContext),
            nameof(InlineLayoutEngine),
            _diagnosticsSession);

        return state with
        {
            CurrentY = state.CurrentY + collapsedTop + childBlock.Height,
            PreviousBottomMargin = margin.Bottom
        };
    }

    private InlineSegmentBuildResult? BuildSegment(
        BlockBox blockContext,
        IReadOnlyList<DisplayNode> inlineChildren,
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
            blockContext.TextAlign ?? HtmlCssConstants.Defaults.TextAlign);

        return new InlineSegmentBuildResult(layout, textLayout.TotalHeight, textLayout.MaxLineWidth);
    }

    private IReadOnlyList<TextRunInput> CollectInlineRuns(
        BlockBox blockContext,
        IReadOnlyList<DisplayNode> inlineChildren,
        float availableWidth,
        bool includeSyntheticListMarker)
    {
        var context = new InlineMeasurementContext(
            blockContext.Style,
            availableWidth,
            _runFactory,
            _textMeasurer,
            _lineHeightStrategy);

        if (includeSyntheticListMarker)
        {
            TryAppendSyntheticListMarkerRun(blockContext, context);
        }

        foreach (var inline in inlineChildren)
        {
            CollectInlineRuns(inline, context);
        }

        return context.Runs;
    }

    private void CollectInlineRuns(
        DisplayNode node,
        InlineMeasurementContext context)
    {
        switch (node)
        {
            case BlockBox block when InlineFlowClassifier.IsAnonymousInlineWrapper(block):
                foreach (var child in block.Children)
                {
                    CollectInlineRuns(child, context);
                }

                return;
        }

        if (_nodeMeasurers.TryMeasure(node, context))
        {
            return;
        }

        if (node is not InlineBox inline)
        {
            return;
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(childInline, context);
        }
    }

    private void TryAppendSyntheticListMarkerRun(
        BlockBox blockContext,
        InlineMeasurementContext context)
    {
        if (blockContext.Role != DisplayRole.ListItem || blockContext.MarkerOffset > 0f || HasExplicitListMarker(blockContext))
        {
            return;
        }

        var markerText = ResolveListMarkerText(blockContext);
        if (string.IsNullOrWhiteSpace(markerText))
        {
            return;
        }

        var marker = new InlineBox(DisplayRole.Inline)
        {
            TextContent = markerText,
            Style = blockContext.Style,
            Parent = blockContext
        };

        context.TryAppendTextRun(marker);
    }

    private static bool HasExplicitListMarker(BlockBox blockContext)
    {
        foreach (var inline in blockContext.Children.OfType<InlineBox>())
        {
            var text = inline.TextContent?.TrimStart();
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            if (text.StartsWith("•", StringComparison.Ordinal) ||
                (char.IsDigit(text[0]) && text.Contains('.')))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveListMarkerText(BlockBox listItem)
    {
        var listContainer = FindNearestListContainer(listItem.Parent);
        if (listContainer is null)
        {
            return string.Empty;
        }

        return ListMarkerResolver.ResolveMarkerText(listContainer, listItem);
    }

    private static DisplayNode? FindNearestListContainer(DisplayNode? node)
    {
        var current = node;
        while (current is not null)
        {
            var tag = current.Element?.TagName;
            if (string.Equals(tag, HtmlCssConstants.HtmlTags.Ul, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tag, HtmlCssConstants.HtmlTags.Ol, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool TryAppendInlineFlowNode(DisplayNode node, ICollection<DisplayNode> pendingInlineFlow)
    {
        if (!InlineFlowClassifier.IsInlineFlowMember(node))
        {
            return false;
        }

        pendingInlineFlow.Add(node);
        return true;
    }

    private static FormattingContextKind ResolveFormattingContext(BlockBox block) =>
        block.IsInlineBlockContext ? FormattingContextKind.InlineBlock : FormattingContextKind.Block;

    private readonly record struct InlineFlowState(
        float CurrentY,
        float PreviousBottomMargin,
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

        public float MeasureWidth(FontKey font, float sizePt, string text) =>
            _metricsProvider.MeasureTextWidth(font, sizePt, text);

        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt) =>
            _metricsProvider.GetMetrics(font, sizePt);
    }
}
