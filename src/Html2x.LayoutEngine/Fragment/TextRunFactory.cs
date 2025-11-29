using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

public sealed class TextRunFactory
{
    private readonly IFontMetricsProvider _metrics;
    private readonly ITextWidthEstimator _widthEstimator;

    public TextRunFactory()
        : this(new FontMetricsProvider(), null)
    {
    }

    private TextRunFactory(IFontMetricsProvider metrics, ITextWidthEstimator? widthEstimator = null)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _widthEstimator = widthEstimator ?? new DefaultTextWidthEstimator(_metrics);
    }

    public TextRun Create(InlineBox inline, BlockBox blockContext)
    {
        if (inline is null)
        {
            throw new ArgumentNullException(nameof(inline));
        }

        if (blockContext is null)
        {
            throw new ArgumentNullException(nameof(blockContext));
        }

        var font = _metrics.GetFontKey(inline.Style);
        var size = _metrics.GetFontSize(inline.Style);
        var text = inline.TextContent ?? string.Empty;
        var color = inline.Style.Color;

        var (ascent, descent) = _metrics.GetMetrics(font, size);
        var width = _widthEstimator.MeasureWidth(font, size, text);

        // Use the caller-provided layout context to anchor inline runs.
        // This keeps the factory agnostic of display hierarchies (inline-block, flex, etc.).
        var origin = new PointF(blockContext.X, blockContext.Y);

        return new TextRun(text, font, size, origin, width, ascent, descent, TextDecorations.None, color);
    }

}
