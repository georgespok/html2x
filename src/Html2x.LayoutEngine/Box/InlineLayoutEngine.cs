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
        var runs = CollectInlineRuns(block, _metrics);

        if (runs.Count == 0)
        {
            return 0f;
        }

        var input = new TextLayoutInput(runs, availableWidth, lineHeight);
        return new TextLayoutEngine(_textMeasurer).Layout(input).TotalHeight;
    }

    private static List<TextRunInput> CollectInlineRuns(DisplayNode block, IFontMetricsProvider metrics)
    {
        var runs = new List<TextRunInput>();
        var runId = 1;
        foreach (var inline in block.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(inline, metrics, runs, ref runId);
        }

        return runs;
    }

    private static void CollectInlineRuns(
        InlineBox inline,
        IFontMetricsProvider metrics,
        ICollection<TextRunInput> runs,
        ref int runId)
    {
        if (IsLineBreak(inline))
        {
            var font = metrics.GetFontKey(inline.Style);
            var fontSize = metrics.GetFontSize(inline.Style);
            runs.Add(new TextRunInput(
                runId++,
                inline,
                string.Empty,
                font,
                fontSize,
                inline.Style,
                PaddingLeft: 0f,
                PaddingRight: 0f,
                MarginLeft: 0f,
                MarginRight: 0f,
                IsLineBreak: true));
            return;
        }

        if (!string.IsNullOrEmpty(inline.TextContent))
        {
            var font = metrics.GetFontKey(inline.Style);
            var fontSize = metrics.GetFontSize(inline.Style);
            var (paddingLeft, paddingRight, marginLeft, marginRight) = GetInlineSpacing(inline);
            runs.Add(new TextRunInput(
                runId++,
                inline,
                inline.TextContent,
                font,
                fontSize,
                inline.Style,
                paddingLeft,
                paddingRight,
                marginLeft,
                marginRight));
        }

        foreach (var childInline in inline.Children.OfType<InlineBox>())
        {
            CollectInlineRuns(childInline, metrics, runs, ref runId);
        }
    }

    private static bool IsLineBreak(InlineBox inline)
        => string.Equals(inline.Element?.TagName, HtmlCssConstants.HtmlTags.Br, StringComparison.OrdinalIgnoreCase);

    private static (float PaddingLeft, float PaddingRight, float MarginLeft, float MarginRight) GetInlineSpacing(InlineBox inline)
    {
        var source = inline;
        if (source.Element is null && source.Parent is InlineBox parent && parent.Element is not null)
        {
            source = parent;
        }

        if (source.Element is null)
        {
            return (0f, 0f, 0f, 0f);
        }

        return (source.Style.Padding.Left, source.Style.Padding.Right, source.Style.Margin.Left, source.Style.Margin.Right);
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
