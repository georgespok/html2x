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
    private readonly BorderShapeDrawer _borderShapeDrawer = new();

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

        var size = page.PageSize;
        canvas.DrawRect(new SKRect(0, 0, size.Width, size.Height), paint);
    }

    private void DrawFragment(SKCanvas canvas, Fragment fragment)
    {
        switch (fragment)
        {
            case TableFragment table:
                DrawTableFragment(canvas, table);
                break;
            case TableRowFragment row:
                DrawTableRowFragment(canvas, row);
                break;
            case TableCellFragment cell:
                DrawTableCellFragment(canvas, cell);
                break;
            case BlockFragment block:
                DrawBlockBackground(canvas, block);
                DrawBlockBorders(canvas, block);
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
                DrawImageBorders(canvas, image);
                break;
            case RuleFragment rule:
                DrawRule(canvas, rule);
                break;
            default:
                throw new NotSupportedException($"Unsupported fragment type: {fragment.GetType().Name}");
        }
    }

    private void DrawTableFragment(SKCanvas canvas, TableFragment table)
    {
        DrawBlockBackground(canvas, table);

        foreach (var row in table.Rows)
        {
            DrawTableRowBackground(canvas, row);
        }

        foreach (var row in table.Rows)
        {
            foreach (var cell in row.Cells)
            {
                DrawTableCellBackground(canvas, cell);
            }
        }

        DrawBlockBorders(canvas, table);

        foreach (var row in table.Rows)
        {
            DrawTableRowBorders(canvas, row);
        }

        foreach (var row in table.Rows)
        {
            foreach (var cell in row.Cells)
            {
                DrawTableCellContent(canvas, cell);
            }
        }
    }

    private void DrawTableRowFragment(SKCanvas canvas, TableRowFragment row)
    {
        DrawBlockBackground(canvas, row);
        DrawBlockBorders(canvas, row);
    }

    private void DrawTableCellFragment(SKCanvas canvas, TableCellFragment cell)
    {
        DrawBlockBackground(canvas, cell);
        DrawBlockBorders(canvas, cell);
        DrawTableCellContent(canvas, cell);
    }

    private static void DrawTableRowBackground(SKCanvas canvas, TableRowFragment row)
    {
        DrawBlockBackground(canvas, row);
    }

    private static void DrawTableCellBackground(SKCanvas canvas, TableCellFragment cell)
    {
        DrawBlockBackground(canvas, cell);
    }

    private void DrawTableRowBorders(SKCanvas canvas, TableRowFragment row)
    {
        DrawBlockBorders(canvas, row);

        foreach (var cell in row.Cells)
        {
            DrawBlockBorders(canvas, cell);
        }
    }

    private void DrawTableCellContent(SKCanvas canvas, TableCellFragment cell)
    {
        foreach (var child in cell.Children)
        {
            DrawFragment(canvas, child);
        }
    }

    private static void DrawBlockBackground(SKCanvas canvas, BlockFragment block)
    {
        var background = block.Style?.BackgroundColor;
        if (background is null || background.Value.A == 0)
        {
            return;
        }

        using var paint = new SKPaint
        {
            Color = new SKColor(background.Value.R, background.Value.G, background.Value.B, background.Value.A),
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };

        canvas.DrawRect(
            new SKRect(block.Rect.Left, block.Rect.Top, block.Rect.Right, block.Rect.Bottom),
            paint);
    }

    private void DrawBlockBorders(SKCanvas canvas, BlockFragment block)
    {
        var borders = block.Style?.Borders;
        if (borders is null || !borders.HasAny)
        {
            return;
        }

        var rect = block.Rect;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        canvas.Save();
        canvas.Translate(rect.Left, rect.Top);

        _borderShapeDrawer.Draw(canvas, new SKSize(rect.Width, rect.Height), borders);

        canvas.Restore();
    }

    private void DrawImageBorders(SKCanvas canvas, ImageFragment image)
    {
        var borders = image.Style?.Borders;
        if (borders is null || !borders.HasAny)
        {
            return;
        }

        var rect = image.Rect;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        canvas.Save();
        canvas.Translate(rect.Left, rect.Top);

        _borderShapeDrawer.Draw(canvas, new SKSize(rect.Width, rect.Height), borders);

        canvas.Restore();
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
