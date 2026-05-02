using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.Renderers.Pdf.Paint;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Drawing;

/// <summary>
/// Draws internal paint commands onto a Skia canvas using existing PDF drawing helpers.
/// </summary>
internal sealed class SkiaPaintCommandDrawer
{
    private readonly ImageRenderer _imageRenderer;
    private readonly SkiaFontCache _fontCache;
    private readonly BorderShapeDrawer _borderShapeDrawer = new();

    public SkiaPaintCommandDrawer(
        PdfRenderSettings settings,
        SkiaFontCache fontCache,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _fontCache = fontCache ?? throw new ArgumentNullException(nameof(fontCache));
        _imageRenderer = new ImageRenderer(settings, diagnosticsSink);
    }

    public void Draw(SKCanvas canvas, IReadOnlyList<PaintCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(commands);

        foreach (var command in commands)
        {
            Draw(canvas, command);
        }
    }

    private void Draw(SKCanvas canvas, PaintCommand command)
    {
        switch (command)
        {
            case PageBackgroundPaintCommand pageBackground:
                DrawPageBackground(canvas, pageBackground);
                break;
            case BackgroundPaintCommand background:
                DrawBackground(canvas, background);
                break;
            case BorderPaintCommand border:
                DrawBorder(canvas, border.Rect, border.Borders);
                break;
            case TextPaintCommand text:
                DrawTextRun(canvas, text.Run);
                break;
            case ImagePaintCommand image:
                _imageRenderer.Render(canvas, image);
                break;
            case RulePaintCommand rule:
                DrawRule(canvas, rule);
                break;
            default:
                throw new NotSupportedException($"Unsupported paint command type: {command.GetType().Name}");
        }
    }

    private static void DrawPageBackground(SKCanvas canvas, PageBackgroundPaintCommand command)
    {
        using var paint = new SKPaint
        {
            Color = ToSkColor(command.Color),
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };

        canvas.DrawRect(new SKRect(0, 0, command.PageSize.Width, command.PageSize.Height), paint);
    }

    private static void DrawBackground(SKCanvas canvas, BackgroundPaintCommand command)
    {
        using var paint = new SKPaint
        {
            Color = ToSkColor(command.Color),
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };

        canvas.DrawRect(
            new SKRect(command.Rect.Left, command.Rect.Top, command.Rect.Right, command.Rect.Bottom),
            paint);
    }

    private void DrawBorder(SKCanvas canvas, System.Drawing.RectangleF rect, BorderEdges borders)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        canvas.Save();
        canvas.Translate(rect.Left, rect.Top);

        _borderShapeDrawer.Draw(canvas, new SKSize(rect.Width, rect.Height), borders);

        canvas.Restore();
    }

    private void DrawTextRun(SKCanvas canvas, TextRun run)
    {
        var color = run.Color ?? ColorRgba.Black;

        using var paint = new SKPaint
        {
            Color = ToSkColor(color),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        using var font = new SKFont(_fontCache.GetTypeface(run), run.FontSizePt)
        {
            Subpixel = true,
            Hinting = SKFontHinting.Full
        };
        if (ShouldEmbolden(run, font.Typeface))
        {
            font.Embolden = true;
        }

        canvas.DrawText(run.Text, run.Origin.X, run.Origin.Y, SKTextAlign.Left, font, paint);
        DrawDecorationsIfAny(canvas, run, paint);
    }

    private static void DrawDecorationsIfAny(SKCanvas canvas, TextRun run, SKPaint textPaint)
    {
        if (run.Decorations == TextDecorations.None)
        {
            return;
        }

        var x0 = run.Origin.X;
        var x1 = run.Origin.X + Math.Max(run.AdvanceWidth, 0);
        var baseline = run.Origin.Y;

        using var paint = new SKPaint
        {
            Color = textPaint.Color,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1f, run.FontSizePt / 16f),
            IsAntialias = true
        };

        if ((run.Decorations & TextDecorations.Underline) != 0)
        {
            var y = baseline + Math.Max(run.Descent / 2f, paint.StrokeWidth * 2);
            canvas.DrawLine(x0, y, x1, y, paint);
        }

        if ((run.Decorations & TextDecorations.LineThrough) != 0)
        {
            var y = baseline - (run.Ascent / 2f);
            canvas.DrawLine(x0, y, x1, y, paint);
        }

        if ((run.Decorations & TextDecorations.Overline) != 0)
        {
            var y = baseline - run.Ascent;
            canvas.DrawLine(x0, y, x1, y, paint);
        }
    }

    private static bool ShouldEmbolden(TextRun run, SKTypeface typeface)
    {
        var requestedWeight = (int)run.Font.Weight;
        if (requestedWeight < (int)FontWeight.W600)
        {
            return false;
        }

        var actualWeight = typeface.FontWeight;
        return actualWeight < requestedWeight;
    }

    private static void DrawRule(SKCanvas canvas, RulePaintCommand command)
    {
        var border = command.Border;
        var color = border?.Color ?? ColorRgba.Black;
        var width = border?.Width ?? 1f;

        using var paint = new SKPaint
        {
            Color = ToSkColor(color),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(width, 0.5f),
            IsAntialias = true
        };

        var y = command.Rect.Top + (command.Rect.Height / 2f);
        canvas.DrawLine(command.Rect.Left, y, command.Rect.Right, y, paint);
    }

    private static SKColor ToSkColor(ColorRgba color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }
}
