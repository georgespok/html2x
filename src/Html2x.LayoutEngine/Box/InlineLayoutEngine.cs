using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public sealed class InlineLayoutEngine(IFontMetricsProvider metrics, ILineHeightStrategy lineHeightStrategy)
    : IInlineLayoutEngine
{
    private readonly ILineHeightStrategy _lineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
    private readonly IFontMetricsProvider _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

    public InlineLayoutEngine()
        : this(new FontMetricsProvider(), new DefaultLineHeightStrategy())
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics)
        : this(metrics, new DefaultLineHeightStrategy())
    {
    }

    public float MeasureHeight(DisplayNode block, float availableWidth)
    {
        if (block is null)
        {
            throw new ArgumentNullException(nameof(block));
        }

        var fontSize = _metrics.GetFontSize(block.Style);
        var font = _metrics.GetFontKey(block.Style);
        var metrics = _metrics.GetMetrics(font, fontSize);

        return _lineHeightStrategy.GetLineHeight(block.Style, font, fontSize, metrics);
    }
}