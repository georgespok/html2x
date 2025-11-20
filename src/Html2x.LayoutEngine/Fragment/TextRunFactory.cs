using System;
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

    public TextRun Create(InlineBox inline)
    {
        if (inline is null)
        {
            throw new ArgumentNullException(nameof(inline));
        }

        var font = _metrics.GetFontKey(inline.Style);
        var size = _metrics.GetFontSize(inline.Style);
        var text = inline.TextContent ?? string.Empty;
        var color = NormalizeColor(inline.Style.Color);

        var (ascent, descent) = _metrics.GetMetrics(font, size);
        var width = _widthEstimator.MeasureWidth(font, size, text);

        var origin = inline.Parent is BlockBox block
            ? new PointF(block.X, block.Y)
            : PointF.Empty;

        return new TextRun(text, font, size, origin, width, ascent, descent, TextDecorations.None, color);
    }

    private static string? NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            return trimmed;
        }

        if (!trimmed.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var start = trimmed.IndexOf('(');
        var end = trimmed.IndexOf(')');
        if (start < 0 || end <= start)
        {
            return trimmed;
        }

        var components = trimmed.Substring(start + 1, end - start - 1)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (components.Length < 3)
        {
            return trimmed;
        }

        if (!byte.TryParse(components[0], out var r) ||
            !byte.TryParse(components[1], out var g) ||
            !byte.TryParse(components[2], out var b))
        {
            return trimmed;
        }

        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
