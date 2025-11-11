using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine;

public sealed class DefaultTextWidthEstimator(IFontMetricsProvider metricsProvider) : ITextWidthEstimator
{
    private readonly IFontMetricsProvider _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));

    public float MeasureWidth(FontKey font, float fontSizePt, string text)
    {
        return _metricsProvider.MeasureTextWidth(font, fontSizePt, text);
    }
}
