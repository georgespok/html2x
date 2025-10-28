using System.Drawing;

namespace Html2x.Core.Layout;

public abstract class Fragment
{
    public RectangleF Rect { get; init; } // absolute page coords (pt)
    public int ZOrder { get; init; } // resolved stacking/z-index
    public VisualStyle Style { get; init; } // minimal style needed to paint this box
}

public sealed record VisualStyle(
    ColorRgba? BackgroundColor = null, // resolved final color; null = transparent
    BorderSide? BorderTop = null,
    BorderSide? BorderRight = null,
    BorderSide? BorderBottom = null,
    BorderSide? BorderLeft = null,
    CornerRadii? CornerRadius = null,
    float Opacity = 1f,
    bool OverflowHidden = false // layout clip intent
);

public sealed record BorderSide(float Width, ColorRgba Color, BorderStyle Style);

public readonly record struct Margins(float Top, float Right, float Bottom, float Left);

public readonly record struct ColorRgba(byte R, byte G, byte B, byte A);

// Optional, if you support <hr> or borders-only shapes
public sealed class RuleFragment : Fragment
{
}

public sealed record FontKey(string Family, FontWeight Weight, FontStyle Style);

public enum FontStyle
{
    Normal,
    Italic,
    Oblique
}

public enum FontWeight : ushort
{
    W100 = 100,
    W200 = 200,
    W300 = 300,
    W400 = 400, // Normal
    W500 = 500,
    W600 = 600,
    W700 = 700, // Bold
    W800 = 800,
    W900 = 900
}

public sealed record Border(float Width, ColorRgba Color, BorderStyle Style);

public enum BorderStyle
{
    None,
    Solid,
    Dashed,
    Dotted
}

public abstract record Brush;

public sealed record SolidBrush(ColorRgba Color) : Brush;

public sealed record LinearGradientBrush(PointF Start, PointF End, GradientStop[] Stops) : Brush;

public sealed record RadialGradientBrush(PointF Center, float Radius, GradientStop[] Stops) : Brush;

public sealed record GradientStop(float Offset01, ColorRgba Color);

public readonly record struct CornerRadii(float TopLeft, float TopRight, float BottomRight, float BottomLeft);