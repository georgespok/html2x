using System.Drawing;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.File;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.Renderers.Pdf.Pipeline;
using Moq;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test;

public class SkiaDeterminismTests
{
    [Fact]
    public async Task RenderTwice_ShouldProduceIdenticalWordGeometry()
    {
        var layout1 = CreateSimpleLayout();
        var layout2 = CreateSimpleLayout();
        var options = new PdfOptions { FontPath = string.Empty };
        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        var renderer = new PdfRenderer(fileDirectory.Object);

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

        layout.Pages.Add(page);
        return layout;
    }

    private static IReadOnlyList<Fragment> CreateSimpleContent()
    {
        var fragments = new List<Fragment>();

        var hello = new TextRun(
            "Hello,",
            new FontKey("Arial", FontWeight.W700, FontStyle.Normal),
            16f,
            new PointF(60, 100),
            40f,
            11f,
            3f);

        var world = new TextRun(
            "Skia!",
            new FontKey("Arial", FontWeight.W700, FontStyle.Normal),
            16f,
            new PointF(110, 100),
            40f,
            11f,
            3f);

        var line = new LineBoxFragment
        {
            Rect = new RectangleF(50, 90, 200, 24),
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
