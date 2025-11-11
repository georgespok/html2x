using Html2x.Abstractions.Layout;

namespace Html2x.LayoutEngine;

public sealed class DefaultTextWidthEstimator : ITextWidthEstimator
{
    private readonly IFontMetricsProvider _metricsProvider;

    public DefaultTextWidthEstimator(IFontMetricsProvider metricsProvider)
    {
        _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));
    }

    public float MeasureWidth(FontKey font, float fontSizePt, string text)
    {
        return _metricsProvider.MeasureTextWidth(font, fontSizePt, text);
    }
}
