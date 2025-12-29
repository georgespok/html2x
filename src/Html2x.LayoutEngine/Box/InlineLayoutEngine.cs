using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public sealed class InlineLayoutEngine : IInlineLayoutEngine
{
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

    private readonly ILineHeightStrategy _lineHeightStrategy;
    private readonly IFontMetricsProvider _metrics;
    private readonly ITextMeasurer _textMeasurer;

    public float MeasureHeight(DisplayNode block, float availableWidth)
    {
        if (block is null)
        {
            throw new ArgumentNullException(nameof(block));
        }

        var fontSize = _metrics.GetFontSize(block.Style);
        var font = _metrics.GetFontKey(block.Style);
        var metrics = _textMeasurer.GetMetrics(font, fontSize);

        return _lineHeightStrategy.GetLineHeight(block.Style, font, fontSize, metrics);
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
