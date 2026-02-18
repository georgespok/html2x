using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Text;

namespace Html2x.LayoutEngine.Box;

public sealed class InlineLayoutEngine : IInlineLayoutEngine
{
    private readonly ILineHeightStrategy _lineHeightStrategy;
    private readonly IFontMetricsProvider _metrics;
    private readonly ITextMeasurer _textMeasurer;

    public InlineLayoutEngine()
        : this(new FontMetricsProvider(), null, new DefaultLineHeightStrategy())
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics)
        : this(metrics, null, new DefaultLineHeightStrategy())
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics, ITextMeasurer? textMeasurer, ILineHeightStrategy lineHeightStrategy)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _textMeasurer = textMeasurer ?? new FallbackTextMeasurer(_metrics);
        _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
    }

    public float MeasureHeight(DisplayNode block, float availableWidth)
    {
        if (block is null)
        {
            throw new ArgumentNullException(nameof(block));
        }

        var fontSize = _metrics.GetFontSize(block.Style);
        var font = _metrics.GetFontKey(block.Style);
        var metrics = _textMeasurer.GetMetrics(font, fontSize);
        var lineHeight = _lineHeightStrategy.GetLineHeight(block.Style, font, fontSize, metrics);
        var runFactory = new InlineRunFactory(_metrics);
        var runs = CollectInlineRuns(block, runFactory, _metrics, _textMeasurer, _lineHeightStrategy, availableWidth);

        if (runs.Count == 0)
        {
            return 0f;
        }

        var input = new TextLayoutInput(runs, availableWidth, lineHeight);
        return new TextLayoutEngine(_textMeasurer).Layout(input).TotalHeight;
    }

    private static List<TextRunInput> CollectInlineRuns(
        DisplayNode block,
        InlineRunFactory runFactory,
        IFontMetricsProvider metrics,
        ITextMeasurer measurer,
        ILineHeightStrategy lineHeightStrategy,
        float availableWidth)
    {
        var runs = new List<TextRunInput>();
        var runId = 1;
        foreach (var inline in block.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(
                inline,
                block.Style,
                runFactory,
                metrics,
                measurer,
                lineHeightStrategy,
                availableWidth,
                runs,
                ref runId);
        }

        return runs;
    }

    private static void CollectInlineRuns(
        InlineBox inline,
        ComputedStyle blockStyle,
        InlineRunFactory runFactory,
        IFontMetricsProvider metrics,
        ITextMeasurer measurer,
        ILineHeightStrategy lineHeightStrategy,
        float availableWidth,
        ICollection<TextRunInput> runs,
        ref int runId)
    {
        if (runFactory.TryBuildInlineBlockLayout(inline, availableWidth, measurer, lineHeightStrategy, out var inlineLayout))
        {
            var margin = inline.Style.Margin.Safe();
            runs.Add(new TextRunInput(
                runId,
                inline,
                string.Empty,
                metrics.GetFontKey(inline.Style),
                metrics.GetFontSize(inline.Style),
                inline.Style,
                PaddingLeft: 0f,
                PaddingRight: 0f,
                MarginLeft: margin.Left,
                MarginRight: margin.Right,
                Kind: TextRunKind.InlineObject,
                InlineObject: inlineLayout));
            runId++;
            return;
        }

        if (runFactory.TryBuildLineBreakRunFromBlockContext(inline, blockStyle, runId, out var lineBreakRun))
        {
            runs.Add(lineBreakRun);
            runId++;
            return;
        }

        if (runFactory.TryBuildTextRun(inline, runId, out var textRun))
        {
            runs.Add(textRun);
            runId++;
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(
                childInline,
                blockStyle,
                runFactory,
                metrics,
                measurer,
                lineHeightStrategy,
                availableWidth,
                runs,
                ref runId);
        }
    }

    private sealed class FallbackTextMeasurer(IFontMetricsProvider metricsProvider) : ITextMeasurer
    {
        private readonly IFontMetricsProvider _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));

        public float MeasureWidth(FontKey font, float sizePt, string text) =>
            _metricsProvider.MeasureTextWidth(font, sizePt, text);

        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt) =>
            _metricsProvider.GetMetrics(font, sizePt);
    }
}
