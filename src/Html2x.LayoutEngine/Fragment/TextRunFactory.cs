using System.Drawing;
using Html2x.Abstractions.Layout;
using Html2x.LayoutEngine.Box;

namespace Html2x.LayoutEngine.Fragment;

public sealed class TextRunFactory
{
    private readonly IFontMetricsProvider _metrics;
    private readonly ITextWidthEstimator _widthEstimator;

    public TextRunFactory()
        : this(new FontMetricsProvider(), null)
    {
    }

    public TextRunFactory(IFontMetricsProvider metrics)
        : this(metrics, null)
    {
    }

    public TextRunFactory(IFontMetricsProvider metrics, ITextWidthEstimator? widthEstimator)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _widthEstimator = widthEstimator ?? new DefaultTextWidthEstimator(_metrics);
    }

    public TextRun Create(InlineBox inline)
    {
        if (inline is null)
        {
            throw new ArgumentNullException(nameof(inline));
        }

        var font = _metrics.GetFontKey(inline.Style);
        var size = _metrics.GetFontSize(inline.Style);
        var text = inline.TextContent ?? string.Empty;

        var (ascent, descent) = _metrics.GetMetrics(font, size);
        var width = _widthEstimator.MeasureWidth(font, size, text);

        var origin = inline.Parent is BlockBox block
            ? new PointF(block.X, block.Y)
            : PointF.Empty;

        return new TextRun(text, font, size, origin, width, ascent, descent);
    }
}