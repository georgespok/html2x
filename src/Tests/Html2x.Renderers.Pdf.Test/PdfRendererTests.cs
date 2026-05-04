using Html2x.RenderModel;
using Html2x.Text;
using Shouldly;
using Html2x.Renderers.Pdf;
using Html2x.Renderers.Pdf.Pipeline;
using UglyToad.PdfPig;

namespace Html2x.Renderers.Pdf.Test;

[Trait("Category", "Integration")]
public class PdfRendererTests
{
    [Fact]
    public async Task RenderAsync_LayoutIsValid_CreatePdfFile()
    {
        // Arrange
        var layout = CreateSimpleLayout();
        var options = new PdfRenderSettings();
        var renderer = new PdfRenderer();

        // Act
        var pdfBytes = await renderer.RenderAsync(layout, options);

        // Assert
        pdfBytes.ShouldNotBeNull();
        PdfValidator.Validate(pdfBytes).ShouldBeTrue();

        using var stream = new MemoryStream(pdfBytes);
        using var pdf = PdfDocument.Open(stream);
        pdf.NumberOfPages.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task RenderAsync_BlockHasChildOffsets_RespectChildOffsets()
    {
        // Arrange
        var layout = CreateLayoutWithOffsetBlock();
        var renderer = new PdfRenderer();
        var options = new PdfRenderSettings();

        // Act
        var pdfBytes = await renderer.RenderAsync(layout, options);

        // Assert
        PdfValidator.Validate(pdfBytes).ShouldBeTrue();

        var words = PdfWordParser.GetRawWords(pdfBytes);
        var edgeWord = PdfWordParser.FindWordByText(words, "Edge");
        var paddedWord = PdfWordParser.FindWordByText(words, "Padding");

        edgeWord.ShouldNotBeNull();
        paddedWord.ShouldNotBeNull();

        var verticalGap = Math.Abs(edgeWord.BoundingBox.Top - paddedWord.BoundingBox.Top);
        verticalGap.ShouldBeGreaterThan(25f); // padding pushes second word further down the page
    }

    [Fact]
    public async Task RenderAsync_FragmentsAreRendered_DoesNotMutateLayoutGeometry()
    {
        var line = CreateLineFragment("Stable", 24, 40, 120, 18);
        var block = new BlockFragment([line])
        {
            Rect = new RectPt(20, 30, 180, 80),
            Style = new VisualStyle()
        };
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(
            new SizePt(300, 300),
            new Spacing(0, 0, 0, 0),
            new List<Fragment> { block },
            1,
            new ColorRgba(255, 255, 255, 255)));
        var renderer = new PdfRenderer();
        var options = new PdfRenderSettings();
        var originalBlockRect = block.Rect;
        var originalLineRect = line.Rect;
        var originalRunOrigin = line.Runs.ShouldHaveSingleItem().Origin;

        var pdfBytes = await renderer.RenderAsync(layout, options);

        PdfValidator.Validate(pdfBytes).ShouldBeTrue();
        layout.Pages.ShouldHaveSingleItem().Children.ShouldHaveSingleItem().ShouldBeSameAs(block);
        block.Rect.ShouldBe(originalBlockRect);
        line.Rect.ShouldBe(originalLineRect);
        line.Runs.ShouldHaveSingleItem().Origin.ShouldBe(originalRunOrigin);
    }

    [Fact]
    public async Task RenderAsync_TextRunWithoutResolvedFont_ThrowsClearException()
    {
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(
            new SizePt(300, 300),
            new Spacing(),
            [
                new LineBoxFragment
                {
                    Rect = new RectPt(0, 0, 120, 20),
                    BaselineY = 15f,
                    LineHeight = 20f,
                    Runs =
                    [
                        new TextRun(
                            "Missing",
                            RendererFontTestData.CreateFont(),
                            12f,
                            new PointPt(0, 15),
                            60f,
                            9f,
                            3f)
                    ]
                }
            ],
            1,
            new ColorRgba(255, 255, 255, 255)));
        var renderer = new PdfRenderer();

        var exception = await Should.ThrowAsync<FontResolutionException>(
            () => renderer.RenderAsync(layout, new PdfRenderSettings()));

        exception.Message.ShouldContain("TextRun.ResolvedFont is required before PDF rendering");
        exception.RequestedFont.ShouldNotBeNull().Family.ShouldBe("Inter");
        exception.Text.ShouldBe("Missing");
    }

    private static HtmlLayout CreateSimpleLayout()
    {
        var layout = new HtmlLayout();

        var page = new LayoutPage(
            new SizePt(PaperSizes.A4.Width, PaperSizes.A4.Height),
            new Spacing(72, 72, 72, 72),
            CreateSimpleContent(),
            1,
            new ColorRgba(255, 255, 255, 255)
        );

        layout.AddPage(page);
        return layout;
    }

    private static IReadOnlyList<Fragment> CreateSimpleContent()
    {
        var fragments = new List<Fragment>();

        var textRun = RendererFontTestData.CreateTextRun(
            "Hello, Html2x!",
            RendererFontTestData.CreateFont(weight: FontWeight.W700),
            12f,
            new PointPt(0, 0),
            80f,
            10f,
            3f);

        var lineBox = new LineBoxFragment
        {
            Rect = new RectPt(0, 0, 400, 20),
            ZOrder = 1,
            Style = new VisualStyle(),
            BaselineY = 15f,
            LineHeight = 20f,
            Runs = [textRun]
        };

        fragments.Add(lineBox);
        return fragments;
    }

    private static HtmlLayout CreateLayoutWithOffsetBlock()
    {
        var layout = new HtmlLayout();

        var block = new BlockFragment([
            CreateLineFragment("Edge", 50, 100, 140, 18),
            CreateLineFragment("Padding", 50, 130, 140, 18)
        ])
        {
            Rect = new RectPt(50, 100, 200, 120),
            Style = new VisualStyle()
        };

        var page = new LayoutPage(
            new SizePt(400, 400),
            new Spacing(0, 0, 0, 0),
            new List<Fragment> { block },
            1,
            new ColorRgba(255, 255, 255, 255));

        layout.AddPage(page);
        return layout;
    }

    private static LineBoxFragment CreateLineFragment(string text, float x, float y, float width, float height)
    {
        var run = RendererFontTestData.CreateTextRun(
            text,
            RendererFontTestData.CreateFont(),
            12f,
            new PointPt(x, y),
            width - 10f,
            9f,
            3f);

        return new LineBoxFragment
        {
            Rect = new RectPt(x, y, width, height),
            ZOrder = 1,
            Style = new VisualStyle(),
            BaselineY = y + height - 6f,
            LineHeight = height,
            Runs = [run]
        };
    }
}
