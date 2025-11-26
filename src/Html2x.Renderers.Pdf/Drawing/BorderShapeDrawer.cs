using Html2x.Abstractions.Layout.Styles;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Drawing;

public class BorderShapeDrawer
{
    /// <summary>
    /// Calculates the drawing rectangles for each border side using Horizontal Dominance strategy.
    /// Top/Bottom take full width. Left/Right fit between them.
    /// </summary>
    public (SKRect Top, SKRect Right, SKRect Bottom, SKRect Left) CalculateRects(SKSize size, BorderEdges borders)
    {
        var w = size.Width;
        var h = size.Height;
        var t = borders.Top?.Width ?? 0;
        var r = borders.Right?.Width ?? 0;
        var b = borders.Bottom?.Width ?? 0;
        var l = borders.Left?.Width ?? 0;

        // Top: Full width (0, 0, W, T)
        var topRect = new SKRect(0, 0, w, t);
        
        // Bottom: Full width (0, H-B, W, H)
        var bottomRect = new SKRect(0, h - b, w, h);
        
        // Right: Height minus Top/Bottom (W-R, T, W, H-B)
        var rightRect = new SKRect(w - r, t, w, h - b);
        
        // Left: Height minus Top/Bottom (0, T, L, H-B)
        var leftRect = new SKRect(0, t, l, h - b);

        return (topRect, rightRect, bottomRect, leftRect);
    }

    public void Draw(SKCanvas canvas, SKSize size, BorderEdges borders)
    {
        var (topRect, rightRect, bottomRect, leftRect) = CalculateRects(size, borders);

        DrawSide(canvas, topRect, borders.Top, isVertical: false);
        DrawSide(canvas, rightRect, borders.Right, isVertical: true);
        DrawSide(canvas, bottomRect, borders.Bottom, isVertical: false);
        DrawSide(canvas, leftRect, borders.Left, isVertical: true);
    }

    private static void DrawSide(SKCanvas canvas, SKRect rect, BorderSide? side, bool isVertical)
    {
        if (side is not { Width: > 0, LineStyle: not BorderLineStyle.None })
        {
            return;
        }

        using var paint = new SKPaint
        {
            Color = new SKColor(side.Color.R, side.Color.G, side.Color.B, side.Color.A),
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Butt,
            StrokeWidth = isVertical ? rect.Width : rect.Height
        };

        using var effect = side.LineStyle switch
        {
            BorderLineStyle.Dashed => SKPathEffect.CreateDash([side.Width * 3, side.Width], 0),
            BorderLineStyle.Dotted => SKPathEffect.CreateDash([side.Width, side.Width], 0),
            _ => null
        };

        if (effect != null)
        {
            paint.PathEffect = effect;
        }

        if (isVertical)
        {
            float midX = rect.Left + rect.Width / 2f;
            canvas.DrawLine(midX, rect.Top, midX, rect.Bottom, paint);
        }
        else
        {
            float midY = rect.Top + rect.Height / 2f;
            canvas.DrawLine(rect.Left, midY, rect.Right, midY, paint);
        }
    }
}
