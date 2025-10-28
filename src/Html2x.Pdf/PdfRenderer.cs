using Html2x.Core.Layout;
using QuestPDF;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Html2x.Pdf;

//public sealed record PdfOptions(
//    SizeF? ForcePageSize = null,
//    IReadOnlyDictionary<FontKey, string>? FontMap = null,
//    ColorRgba? DefaultTextColor = null
//);

public class PdfRenderer
{
    public Task<byte[]> RenderAsync(HtmlLayout htmlLayout, PdfOptions options)
    {
        ConfigureQuestPdf(options.ForntPath);

        if (htmlLayout is null)
        {
            throw new ArgumentNullException(nameof(htmlLayout));
        }

        options ??= new PdfOptions();

        var bytes = RenderWithQuestPdf(htmlLayout, options);
        return Task.FromResult(bytes);
    }

    private static byte[] RenderWithQuestPdf(HtmlLayout layout, PdfOptions options)
    {
        using var stream = new MemoryStream();

        Document.Create(doc =>
            {
                foreach (var page in layout.Pages)
                {
                    var pageSize = page.Size;

                    doc.Page(p =>
                    {
                        p.Size(new PageSize(pageSize.Width, pageSize.Height));
                        p.Margin(0); // we’re not using page margins in the MVP fluent version

                        // Optional page background (solid color only for MVP)
                        var bg = page.PageBackground ?? new ColorRgba(255, 255, 255, 255);

                        //p.Background().Rectangle().Fill(bg.ToQuestColor());

                        // Content: render all fragments as a flowing column
                        p.Content().Column(col =>
                        {
                            foreach (var fragment in page.Children)
                            {
                                RenderFragmentAsFlow(col, fragment, options);
                            }
                        });
                    });
                }
            })
            .GeneratePdf(stream);

        return stream.ToArray();
    }

    // ---- Flow rendering (MVP) ----
    private static void RenderFragmentAsFlow(ColumnDescriptor col, Fragment fragment, PdfOptions options)
    {
        switch (fragment)
        {
            case BlockFragment block:
                // Represent a block as a sub-column to keep children grouped.
                col.Item().Column(inner =>
                {
                    // (Optional) you might map block background/borders here using Container decoration later
                    foreach (var child in block.Children)
                    {
                        RenderFragmentAsFlow(inner, child, options);
                    }
                });
                break;

            case LineBoxFragment line:
                // One paragraph per line box
                col.Item().Text(t =>
                {
                    // (Optional) you can map line-level style here (e.g., background via Decoration, later)
                    foreach (var run in line.Runs)
                    {
                        AppendSpan(t, run, options);
                    }
                });
                break;

            case ImageFragment img:
                // MVP placeholder; later map ImageRef to actual image bytes and call .Image(...)
                col.Item().Text("🖼️ [image]");
                break;

            case RuleFragment:
                // Simple horizontal rule MVP
                col.Item().LineHorizontal(1);
                break;

            default:
                // Unknown: ignore or emit a placeholder
                col.Item().Text("[unknown fragment]");
                break;
        }
    }

    private static void AppendSpan(TextDescriptor text, TextRun run, PdfOptions options)
    {
        //var fontFamily = ;
        //var color = (options.DefaultTextColor ?? new ColorRgba(0, 0, 0, 255)).ToQuestColor();

        //var color = Color.FromRGB(0, 0, 0);

        text.Span(run.Text)
            //.FontFamily(fontFamily)
            .FontSize(run.FontSizePt)
            //.FontColor(color)
            .Bold()
            .Italic(run.Font.Style != FontStyle.Normal)
            .Underline(run.Decorations.HasFlag(TextDecorations.Underline))
            .Strikethrough(run.Decorations.HasFlag(TextDecorations.LineThrough));

        // We intentionally ignore Origin/AdvanceWidth/Ascent/Descent here:
        // the fluent Text engine does layout for us in this MVP.
    }

    // ---- Utilities ----
    private static string ResolveFontFamily(IReadOnlyDictionary<FontKey, string>? map, FontKey key)
    {
        return map != null && map.TryGetValue(key, out var name)
            ? name
            : key.Family ?? "Times New Roman";
    }

    private static void ConfigureQuestPdf(string fontPath)
    {
        Settings.License = LicenseType.Community;

        if (!string.IsNullOrWhiteSpace(fontPath) && File.Exists(fontPath))
        {
            Settings.UseEnvironmentFonts = false;
            using var fontStream = File.OpenRead(fontPath);
            FontManager.RegisterFont(fontStream);
        }
        else
        {
            Settings.UseEnvironmentFonts = true;
        }
    }
}

// ---- Helpers to bridge your ColorRgba to QuestPDF Color ----
//internal static class ColorHelpers
//{
//    public static Color ToQuestColor(this ColorRgba c) => new Color(c.R, c.G, c.B, c.A);
//}