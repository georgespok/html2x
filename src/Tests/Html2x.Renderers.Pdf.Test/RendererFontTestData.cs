using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Text;

namespace Html2x.Renderers.Pdf.Test;

internal static class RendererFontTestData
{
    public static string FontPath { get; } = FindFontPath();

    public static FontKey CreateFont(string family = "Inter", FontWeight weight = FontWeight.W400,
        FontStyle style = FontStyle.Normal) =>
        new(family, weight, style);

    public static ResolvedFont CreateResolvedFont(FontKey font) =>
        new(
            font.Family,
            font.Weight,
            font.Style,
            FontPath,
            FontPath,
            ConfiguredPath: FontPath);

    public static TextRun CreateTextRun(
        string text,
        FontKey font,
        float sizePt,
        PointPt origin,
        float advanceWidth,
        float ascent,
        float descent) =>
        new(
            text,
            font,
            sizePt,
            origin,
            advanceWidth,
            ascent,
            descent,
            ResolvedFont: CreateResolvedFont(font));

    private static string FindFontPath()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Html2x.Test",
            "Fonts",
            "Inter-Regular.ttf"));

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Test font file not found: {path}");
        }

        return path;
    }
}