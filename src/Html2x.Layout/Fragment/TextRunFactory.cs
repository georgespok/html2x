using System.Drawing;
using Html2x.Core.Layout;
using Html2x.Layout.Box;

namespace Html2x.Layout.Fragment;

public sealed class TextRunFactory
{
    private readonly FontMetricsProvider _metrics = new();

    public TextRun Create(InlineBox inline)
    {
        var font = new FontKey(inline.Style.FontFamily, FontWeight.W400, FontStyle.Normal);
        var size = inline.Style.FontSizePt;
        var text = inline.TextContent ?? string.Empty;
        var width = text.Length * size * 0.5f; // simplistic width estimate

        var (ascent, descent) = _metrics.Get(font, size);

        return new TextRun(
            text,
            font,
            size,
            new PointF(inline.Parent is BlockBox b ? b.X : 0, inline.Parent is BlockBox bb ? bb.Y : 0),
            width,
            ascent,
            descent
        );
    }
}