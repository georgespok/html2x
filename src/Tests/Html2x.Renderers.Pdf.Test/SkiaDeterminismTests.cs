using Html2x.Renderers.Pdf.Pipeline;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test;

[Trait("Category", "Integration")]
public class SkiaDeterminismTests
{
    [Fact]
    public async Task RenderTwice_ProduceIdenticalWordGeometry()
    {
        var layout1 = CreateSimpleLayout();
        var layout2 = CreateSimpleLayout();
        var options = new PdfRenderSettings();
        var renderer = new PdfRenderer();

        // Warm up renderer/font caches to avoid first-render variance.
        await renderer.RenderAsync(CreateSimpleLayout(), options);

        var pdf1 = await renderer.RenderAsync(layout1, options);
        var pdf2 = await renderer.RenderAsync(layout2, options);

        var words1 = PdfWordParser.GetRawWords(pdf1)
            .Select(w => (w.Text, Rect: w.BoundingBox))
            .ToList();

        var words2 = PdfWordParser.GetRawWords(pdf2)
            .Select(w => (w.Text, Rect: w.BoundingBox))
            .ToList();

        words1.Count.ShouldBe(words2.Count);
        for (var i = 0; i < words1.Count; i++)
        {
            words1[i].Text.ShouldBe(words2[i].Text);
            words1[i].Rect.ShouldBe(words2[i].Rect);
        }
    }

    private static HtmlLayout CreateSimpleLayout()
    {
        var layout = new HtmlLayout();

        var page = new LayoutPage(
            new SizePt(PaperSizes.A4.Width, PaperSizes.A4.Height),
            new Spacing(50, 50, 50, 50),
            CreateSimpleContent(),
            1,
            new ColorRgba(255, 255, 255, 255));

        layout.AddPage(page);
        return layout;
    }

    private static IReadOnlyList<Fragment> CreateSimpleContent()
    {
        var fragments = new List<Fragment>();

        var hello = RendererFontTestData.CreateTextRun(
            "Hello,",
            RendererFontTestData.CreateFont(weight: FontWeight.W700),
            16f,
            new PointPt(60, 100),
            40f,
            11f,
            3f);

        var world = RendererFontTestData.CreateTextRun(
            "Skia!",
            RendererFontTestData.CreateFont(weight: FontWeight.W700),
            16f,
            new PointPt(110, 100),
            40f,
            11f,
            3f);

        var line = new LineBoxFragment
        {
            Rect = new RectPt(50, 90, 200, 24),
            ZOrder = 1,
            Style = new VisualStyle(),
            BaselineY = 110,
            LineHeight = 24,
            Runs = [hello, world]
        };

        fragments.Add(line);
        return fragments;
    }
}
