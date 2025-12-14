using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Options;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Drawing;

/// <summary>
/// Draws layout fragments onto an <see cref="SKCanvas"/> using absolute page coordinates.
/// </summary>
internal sealed class SkiaFragmentDrawer
{
    private readonly PdfOptions _options;
    private readonly DiagnosticsSession? _diagnosticsSession;
    private readonly ImageRenderer _imageRenderer;
    private readonly SkiaFontCache _fontCache;

    public SkiaFragmentDrawer(PdfOptions options, DiagnosticsSession? diagnosticsSession, SkiaFontCache fontCache)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _diagnosticsSession = diagnosticsSession;
        _fontCache = fontCache ?? throw new ArgumentNullException(nameof(fontCache));
        _imageRenderer = new ImageRenderer(_options, diagnosticsSession);
    }

    public void DrawPage(SKCanvas canvas, LayoutPage page)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        DrawPageBackground(canvas, page);

        foreach (var fragment in page.Children)
        {
            DrawFragment(canvas, fragment);
        }
    }

    private static void DrawPageBackground(SKCanvas canvas, LayoutPage page)
    {
        var bg = page.PageBackground ?? new ColorRgba(255, 255, 255, 255);

        using var paint = new SKPaint
        {
            Color = new SKColor(bg.R, bg.G, bg.B, bg.A),
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };

        canvas.DrawRect(new SKRect(0, 0, page.Size.Width, page.Size.Height), paint);
    }

    private void DrawFragment(SKCanvas canvas, Fragment fragment)
    {
        switch (fragment)
        {
            case BlockFragment block:
                foreach (var child in block.Children)
                {
                    DrawFragment(canvas, child);
                }
                break;
            case LineBoxFragment line:
                DrawLineBox(canvas, line);
                break;
            case ImageFragment image:
                _imageRenderer.Render(canvas, image);
                break;
            case RuleFragment rule:
                DrawRule(canvas, rule);
                break;
            default:
                throw new NotSupportedException($"Unsupported fragment type: {fragment.GetType().Name}");
        }
    }

    private void DrawLineBox(SKCanvas canvas, LineBoxFragment line)
    {
        var runs = line.Runs;
        if (runs.Count == 0)
        {
            return;
        }

        foreach (var run in runs)
        {
            DrawTextRun(canvas, run);
        }
    }

    private void DrawTextRun(SKCanvas canvas, TextRun run)
    {
        var color = run.Color ?? ColorRgba.Black;

        using var paint = new SKPaint
        {
            Color = new SKColor(color.R, color.G, color.B, color.A),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        using var font = new SKFont(_fontCache.GetTypeface(run.Font), run.FontSizePt)
        {
            Subpixel = true,
            Hinting = SKFontHinting.Full
        };

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

    private static void DrawRule(SKCanvas canvas, RuleFragment rule)
    {
        var border = rule.Style?.Borders?.Top;
        var color = border?.Color ?? ColorRgba.Black;
        var width = border?.Width ?? 1f;

        using var paint = new SKPaint
        {
            Color = new SKColor(color.R, color.G, color.B, color.A),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(width, 0.5f),
            IsAntialias = true
        };

        var y = rule.Rect.Top + (rule.Rect.Height / 2f);
        canvas.DrawLine(rule.Rect.Left, y, rule.Rect.Right, y, paint);
    }
}
