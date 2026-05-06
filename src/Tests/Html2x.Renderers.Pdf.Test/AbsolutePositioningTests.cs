using Html2x.Renderers.Pdf.Pipeline;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test;

[Trait("Category", "Integration")]
public sealed class AbsolutePositioningTests
{
    [Fact]
    public async Task RenderAsync_PositionedLines_PreservesPageCoordinates()
    {
        var layout = new HtmlLayout();
        layout.AddPage(new(
            new(400f, 400f),
            new(),
            [
                CreateLine("Anchor", 40f, 60f),
                CreateLine("Offset", 180f, 220f)
            ],
            1,
            new ColorRgba(255, 255, 255, 255)));
        var renderer = new PdfRenderer();

        var pdfBytes = await renderer.RenderAsync(layout, new());

        PdfValidator.Validate(pdfBytes).ShouldBeTrue();
        var words = PdfWordParser.GetRawWords(pdfBytes);
        var anchor = PdfWordParser.FindWordByText(words, "Anchor").ShouldNotBeNull();
        var offset = PdfWordParser.FindWordByText(words, "Offset").ShouldNotBeNull();
        offset.BoundingBox.Left.ShouldBeGreaterThan(anchor.BoundingBox.Left + 100d);
        Math.Abs(offset.BoundingBox.Top - anchor.BoundingBox.Top).ShouldBeGreaterThan(100d);
    }

    private static LineBoxFragment CreateLine(string text, float x, float y) =>
        new()
        {
            Rect = new(x, y, 100f, 20f),
            BaselineY = y + 15f,
            LineHeight = 20f,
            Runs =
            [
                RendererFontTestData.CreateTextRun(
                    text,
                    RendererFontTestData.CreateFont(weight: FontWeight.W400),
                    12f,
                    new(x, y + 15f),
                    60f,
                    9f,
                    3f)
            ]
        };
}