using AngleSharp;
using AngleSharp.Dom;
using Html2x.Layout.Style;
using Html2x.Layout.Test.Assertions;

namespace Html2x.Layout.Test;

public class CssStyleComputerTests
{
    private readonly CssStyleComputer _sut = new();

    [Fact]
    public async Task Compute_ShouldProjectComputedStyles()
    {
        // Arrange

        const string redRgba = "rgba(255, 0, 0, 1)"; // red #ff0000
        const float bodyFontSize = 18f; // 18pt

        var document = await CreateHtmlDocument(@"
            <html>
              <body style='margin: 96px; font-family: Helvetica; font-size: 18pt; color: #ff0000;'>
                <h1 style='font-weight: 700; font-size: 30pt; text-align: Right;'>Heading</h1>
                <p style='margin-top: 12pt;'>Paragraph</p>
                <div>Div</div>
              </body>
            </html>");

        // Act
        var tree = _sut.Compute(document);

        // Assert via fluent helpers
        var root = tree
            .AssertThat()
            .HavePageMargins(72f, 72f, 72f, 72f)
            .Root()
            .HasTag("BODY")
            .Style(s => s
                .FontFamily("Helvetica")
                .FontSize(bodyFontSize)
                .Color(redRgba))
            .HasChildrenCount(3);

        root
            .Child("H1")
            .HasTag("H1")
            .Style(s => s
                .Bold(true)
                .FontSize(30f)
                .TextAlign("right")
                .Color(redRgba));

        root
            .Child("P")
            .HasTag("P")
            .Style(s => s
                .MarginTop(12f)
                .FontSize(bodyFontSize)
                .Color(redRgba)
                .Bold(false));

        root.Child("DIV")
            .HasTag("DIV")
            .Style(s => s
                .MarginTop(0)
                .FontSize(bodyFontSize)
                .Color(redRgba));
    }

    private static async Task<IDocument> CreateHtmlDocument(string html)
    {
        var context = BrowsingContext.New(Configuration.Default.WithCss());
        var document = await context.OpenAsync(req => req.Content(html));
        return document;
    }
}
