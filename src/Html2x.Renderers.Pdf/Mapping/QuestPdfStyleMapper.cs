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

        if (run.Color is { } color)
        {
            textSpanDescriptor.FontColor(Map(color));
        }
    }

    public static Color Map(ColorRgba color)
    {
        return Color.FromRGB(color.R, color.G, color.B)
            .WithAlpha(color.A / 255f);
    }

}
