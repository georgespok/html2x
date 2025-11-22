using System.Drawing;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Shouldly;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.Renderers.Pdf.Pipeline;

namespace Html2x.Renderers.Pdf.Test;

public class PdfRendererTests
{
    [Fact]
    public async Task RenderAsync_ShouldCreatePdfFile()
    {
        // Arrange
        var layout = CreateSimpleLayout();
        var options = new PdfOptions { FontPath = string.Empty };
        var renderer = new PdfRenderer();

        // Act
        var pdfBytes = await renderer.RenderAsync(layout, options);

        // Assert
        pdfBytes.ShouldNotBeNull();
        PdfValidator.Validate(pdfBytes).ShouldBeTrue();

        var words = PdfWordParser.GetStyledWords(pdfBytes);
        words[0].Text.ShouldBe("Hello,");
        words[1].Text.ShouldBe("Html2x!");
        words[0].FontSize.ShouldBe(12f);
        words[1].FontSize.ShouldBe(12f);
        words[0].IsBold.ShouldBeTrue();
        words[1].IsBold.ShouldBeTrue();
    }

    [Fact]
    public async Task RenderAsync_BlockRespectsChildOffsets()
    {
        // Arrange
        var layout = CreateLayoutWithOffsetBlock();
        var renderer = new PdfRenderer();
        var options = new PdfOptions { FontPath = string.Empty };

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

    private static HtmlLayout CreateSimpleLayout()
    {
        var layout = new HtmlLayout();

        var page = new LayoutPage(
            new SizeF(PaperSizes.A4.Width, PaperSizes.A4.Height),
            new Spacing(72, 72, 72, 72),
            CreateSimpleContent(),
            1,
            new ColorRgba(255, 255, 255, 255)
        );

        layout.Pages.Add(page);
        return layout;
    }

    private static IReadOnlyList<Fragment> CreateSimpleContent()
    {
        var fragments = new List<Fragment>();

        var textRun = new TextRun(
            "Hello, Html2x!",
            new FontKey("Arial", FontWeight.W700, FontStyle.Normal),
            12f,
            new PointF(0, 0),
            80f,
            10f,
            3f
        );

        var lineBox = new LineBoxFragment
        {
            Rect = new RectangleF(0, 0, 400, 20),
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

        var block = new BlockFragment
        {
            Rect = new RectangleF(50, 100, 200, 120),
            Style = new VisualStyle(),
            Children =
            {
                CreateLineFragment("Edge", 50, 100, 140, 18),
                CreateLineFragment("Padding", 50, 130, 140, 18)
            }
        };

        var page = new LayoutPage(
            new SizeF(400, 400),
            new Spacing(0, 0, 0, 0),
            new List<Fragment> { block },
            1,
            new ColorRgba(255, 255, 255, 255));

        layout.Pages.Add(page);
        return layout;
    }

    private static LineBoxFragment CreateLineFragment(string text, float x, float y, float width, float height)
    {
        var run = new TextRun(
            text,
            new FontKey("Arial", FontWeight.W400, FontStyle.Normal),
            12f,
            new PointF(x, y),
            width - 10f,
            9f,
            3f);

        return new LineBoxFragment
        {
            Rect = new RectangleF(x, y, width, height),
            ZOrder = 1,
            Style = new VisualStyle(),
            BaselineY = y + height - 6f,
            LineHeight = height,
            Runs = [run]
        };
    }
}


