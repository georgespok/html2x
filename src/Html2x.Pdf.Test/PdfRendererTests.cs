using System.Drawing;
using Html2x.Core;
using Html2x.Core.Layout;
using Shouldly;

namespace Html2x.Pdf.Test;

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

    private static HtmlLayout CreateSimpleLayout()
    {
        var layout = new HtmlLayout();

        var page = new LayoutPage(
            new SizeF(PaperSizes.A4.Width, PaperSizes.A4.Height),
            new Margins(72, 72, 72, 72),
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
}
