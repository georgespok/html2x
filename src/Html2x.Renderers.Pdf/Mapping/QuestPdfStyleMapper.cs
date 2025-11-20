using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using FontWeight = Html2x.Abstractions.Layout.Styles.FontWeight;

namespace Html2x.Renderers.Pdf.Mapping;

/// <summary>
///     Helper for mapping style and font attributes to QuestPDF API.
/// </summary>
internal static class QuestPdfStyleMapper
{
    public static void ApplyTextStyle(TextDescriptor text, TextRun run)
    {
        var textSpanDescriptor = text.Span(run.Text)
            .FontSize(run.FontSizePt)
            .Italic(run.Font.Style != FontStyle.Normal)
            .Underline(run.Decorations.HasFlag(TextDecorations.Underline))
            .Strikethrough(run.Decorations.HasFlag(TextDecorations.LineThrough));

        if (run.Font.Weight >= FontWeight.W600)
        {
            textSpanDescriptor.Bold();
        }

        if (!string.IsNullOrWhiteSpace(run.ColorHex))
        {
            textSpanDescriptor.FontColor(Map(run.ColorHex!));
        }
    }

    public static Color Map(ColorRgba color)
    {
        return Color.FromRGB(color.R, color.G, color.B)
            .WithAlpha(color.A / 255f);
    }

    private static Color Map(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return Color.FromRGB(0, 0, 0);
        }

        var trimmed = hex.Trim();
        if (!trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            return Color.FromRGB(0, 0, 0);
        }

        var value = trimmed.TrimStart('#');
        byte r = 0, g = 0, b = 0, a = 255;

        if (value.Length >= 6)
        {
            r = Convert.ToByte(value[..2], 16);
            g = Convert.ToByte(value.Substring(2, 2), 16);
            b = Convert.ToByte(value.Substring(4, 2), 16);
        }

        if (value.Length == 8)
        {
            a = Convert.ToByte(value.Substring(6, 2), 16);
        }

        return Color.FromRGB(r, g, b).WithAlpha(a / 255f);
    }
}
