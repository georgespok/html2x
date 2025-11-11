namespace Html2x.LayoutEngine.Box;

public sealed class InlineLayoutEngine : IInlineLayoutEngine
{
    private readonly ILineHeightStrategy _lineHeightStrategy;
    private readonly IFontMetricsProvider _metrics;

    public InlineLayoutEngine()
        : this(new FontMetricsProvider(), new DefaultLineHeightStrategy())
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics)
        : this(metrics, new DefaultLineHeightStrategy())
    {
    }

    public InlineLayoutEngine(IFontMetricsProvider metrics, ILineHeightStrategy lineHeightStrategy)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
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
        var metrics = _metrics.GetMetrics(font, fontSize);

        return _lineHeightStrategy.GetLineHeight(block.Style, font, fontSize, metrics);
    }
}