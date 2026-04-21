using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Text;

namespace Html2x.LayoutEngine.Box;

public sealed class InlineLayoutEngine : IInlineLayoutEngine
{
    private readonly ILineHeightStrategy _lineHeightStrategy;
    private readonly IFontMetricsProvider _metrics;
    private readonly ITextMeasurer _textMeasurer;
    private readonly InlineRunFactory _runFactory;
    private readonly TextLayoutEngine _textLayout;
    private readonly InlineLayoutResultBuilder _layoutResultBuilder;
    private readonly InlineNodeMeasurerRegistry _nodeMeasurers;

    public InlineLayoutEngine()
        : this(
            new FontMetricsProvider(),
            null,
            new DefaultLineHeightStrategy(),
            new BlockFormattingContext(),
            InlineNodeMeasurerRegistry.CreateDefault())
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics)
        : this(
            metrics,
            null,
            new DefaultLineHeightStrategy(),
            new BlockFormattingContext(),
            InlineNodeMeasurerRegistry.CreateDefault())
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics, ITextMeasurer? textMeasurer, ILineHeightStrategy lineHeightStrategy)
        : this(
            metrics,
            textMeasurer,
            lineHeightStrategy,
            new BlockFormattingContext(),
            InlineNodeMeasurerRegistry.CreateDefault())
    {
    }

    internal InlineLayoutEngine(
        IFontMetricsProvider metrics,
        ITextMeasurer? textMeasurer,
        ILineHeightStrategy lineHeightStrategy,
        IBlockFormattingContext blockFormattingContext,
        InlineNodeMeasurerRegistry nodeMeasurers,
        IImageLayoutResolver? imageResolver = null)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _textMeasurer = textMeasurer ?? new FallbackTextMeasurer(_metrics);
        _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
        _runFactory = new InlineRunFactory(_metrics, blockFormattingContext, imageResolver);
        _textLayout = new TextLayoutEngine(_textMeasurer);
        _layoutResultBuilder = new InlineLayoutResultBuilder(_textMeasurer);
        _nodeMeasurers = nodeMeasurers ?? throw new ArgumentNullException(nameof(nodeMeasurers));
    }

    public InlineLayoutResult Layout(BlockBox block, InlineLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(block);

        var segments = new List<InlineFlowSegmentLayout>();
        var pendingInlineFlow = new List<DisplayNode>();
        var currentY = request.ContentTop;
        var previousBottomMargin = 0f;
        var maxLineWidth = 0f;
        var includeSyntheticListMarker = request.IncludeSyntheticListMarker;

        foreach (var child in block.Children)
        {
            if (TryAppendInlineFlowNode(child, pendingInlineFlow))
            {
                continue;
            }

            currentY = FlushPendingInlineFlow(
                block,
                request,
                pendingInlineFlow,
                currentY,
                ref previousBottomMargin,
                ref includeSyntheticListMarker,
                segments,
                ref maxLineWidth);

            switch (child)
            {
                case TableBox table:
                {
                    var margin = table.Style.Margin.Safe();
                    var collapsedTop = Math.Max(previousBottomMargin, margin.Top);
                    currentY += collapsedTop + table.Height;
                    previousBottomMargin = margin.Bottom;
                    break;
                }
                case BlockBox childBlock:
                {
                    var margin = childBlock.Style.Margin.Safe();
                    var collapsedTop = Math.Max(previousBottomMargin, margin.Top);
                    currentY += collapsedTop + childBlock.Height;
                    previousBottomMargin = margin.Bottom;
                    break;
                }
            }
        }

        currentY = FlushPendingInlineFlow(
            block,
            request,
            pendingInlineFlow,
            currentY,
            ref previousBottomMargin,
            ref includeSyntheticListMarker,
            segments,
            ref maxLineWidth);

        var result = new InlineLayoutResult(
            segments,
            Math.Max(0f, currentY + previousBottomMargin - request.ContentTop),
            maxLineWidth);
        block.InlineLayout = result;
        return result;
    }

    private float FlushPendingInlineFlow(
        BlockBox blockContext,
        InlineLayoutRequest request,
        List<DisplayNode> pendingInlineFlow,
        float currentY,
        ref float previousBottomMargin,
        ref bool includeSyntheticListMarker,
        ICollection<InlineFlowSegmentLayout> segments,
        ref float maxLineWidth)
    {
        if (pendingInlineFlow.Count == 0)
        {
            return currentY;
        }

        currentY += previousBottomMargin;
        previousBottomMargin = 0f;

        var segment = BuildSegment(
            blockContext,
            pendingInlineFlow,
            request.AvailableWidth,
            request.ContentLeft,
            currentY,
            includeSyntheticListMarker);

        pendingInlineFlow.Clear();
        includeSyntheticListMarker = false;

        if (segment is null)
        {
            return currentY;
        }

        segments.Add(segment.Value.Layout);
        maxLineWidth = Math.Max(maxLineWidth, segment.Value.MaxLineWidth);
        return currentY + segment.Value.Height;
    }

    private InlineSegmentBuildResult? BuildSegment(
        BlockBox blockContext,
        IReadOnlyList<DisplayNode> inlineChildren,
        float availableWidth,
        float contentLeft,
        float contentTop,
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
        var textLayout = _textLayout.Layout(new TextLayoutInput(runs, availableWidth, lineHeight));
        var layout = _layoutResultBuilder.BuildSegment(
            blockContext,
            textLayout,
            contentLeft,
            contentTop,
            availableWidth,
            blockContext.TextAlign ?? HtmlCssConstants.Defaults.TextAlign);

        return new InlineSegmentBuildResult(layout, textLayout.TotalHeight, textLayout.MaxLineWidth);
    }

    private List<TextRunInput> CollectInlineRuns(
        BlockBox blockContext,
        IReadOnlyList<DisplayNode> inlineChildren,
        float availableWidth,
        bool includeSyntheticListMarker)
    {
        var runs = new List<TextRunInput>();
        var runId = 1;
        var context = new InlineMeasurementContext(
            blockContext.Style,
            availableWidth,
            _runFactory,
            _textMeasurer,
            _lineHeightStrategy);

        if (includeSyntheticListMarker)
        {
            TryAppendSyntheticListMarkerRun(blockContext, context, runs, ref runId);
        }

        foreach (var inline in inlineChildren)
        {
            CollectInlineRuns(inline, context, runs, ref runId);
        }

        return runs;
    }

    private void CollectInlineRuns(
        DisplayNode node,
        InlineMeasurementContext context,
        ICollection<TextRunInput> runs,
        ref int runId)
    {
        switch (node)
        {
            case BlockBox block when IsAnonymousInlineWrapper(block):
                foreach (var child in block.Children)
                {
                    CollectInlineRuns(child, context, runs, ref runId);
                }

                return;
        }

        if (_nodeMeasurers.TryMeasure(node, context, runs, ref runId))
        {
            return;
        }

        if (node is not InlineBox inline)
        {
            return;
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(childInline, context, runs, ref runId);
        }
    }

    private void TryAppendSyntheticListMarkerRun(
        BlockBox blockContext,
        InlineMeasurementContext context,
        ICollection<TextRunInput> runs,
        ref int runId)
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

        context.TryAppendTextRun(marker, runs, ref runId);
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
        switch (node)
        {
            case InlineBox:
            case InlineBlockBoundaryBox:
                pendingInlineFlow.Add(node);
                return true;
            case BlockBox block when IsAnonymousInlineWrapper(block):
                pendingInlineFlow.Add(block);
                return true;
            default:
                return false;
        }
    }

    private static bool IsAnonymousInlineWrapper(BlockBox block)
    {
        return block.IsAnonymous &&
               block.Children.Count > 0 &&
               block.Children.All(static child => child is InlineBox);
    }

    private readonly record struct InlineSegmentBuildResult(
        InlineFlowSegmentLayout Layout,
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
