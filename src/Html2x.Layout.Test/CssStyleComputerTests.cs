using AngleSharp;
using AngleSharp.Dom;
using Html2x.Layout.Style;
using Html2x.Layout.Test.Assertions;

namespace Html2x.Layout.Test;

public class CssStyleComputerTests
{
    private readonly CssStyleComputer _sut = new(new StyleTraversal(), new UserAgentDefaults());

    [Fact]
    public async Task Compute_ShouldProjectComputedStyles()
    {
        // Arrange

        const string redRgba = "rgba(255, 0, 0, 1)"; // red #ff0000
        const float bodyFontSize = 18f; // 18pt
        const string fontHelvetica = "Helvetica"; 

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

        var actual = StyleTreeSnapshot.FromTree(tree);

        actual.ShouldMatch(new ("body",
            new() { FontFamily = fontHelvetica, FontSizePt = bodyFontSize, Color = redRgba},
            [
                new ("h1", new()
                {
                    FontFamily = fontHelvetica, FontSizePt = 30, Color = redRgba,
                    TextAlign = "right", Bold = true
                }),
                new ("p", new() { FontFamily = fontHelvetica, FontSizePt = bodyFontSize, Color = redRgba }),
                new ("div", new() { FontFamily = fontHelvetica, FontSizePt = bodyFontSize, Color = redRgba })
            ]));
    }

    [Fact]
    public async Task Compute_WithNestedBlocksAndInlines_ProducesExpectedTree()
    {
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div>
                    <span>Span inside Div</span>
                    <p>Paragraph inside Div
                        <div>Nested Div inside Paragraph
                            <span>Nested Span inside nested Div</span>
                        </div>
                    </p>
                </div>
            </body></html>");

        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // AngleSharp auto-closes <p> when it encounters <div> inside it (invalid HTML nesting).
        // This results in: <p>Paragraph inside Div</p><div>...</div><p></p>
        actual.ShouldMatch(new("body", null, [
            new("div", null, 
            [
                new("span"),  // Span inside Div
                new("p"),      // Paragraph inside Div (auto-closed before nested div)
                new("div", null,     // Nested Div inside Paragraph (now sibling, not nested)
                [
                    new("span") // Nested Span inside nested Div
                ]),
                new("p")       // Empty <p> created by parser after closing div
            ])
        ]));
    }

    [Fact]
    public async Task Compute_WithListItems_ProducesExpectedTree()
    {
        var document = await CreateHtmlDocument(
            @"<html><body>
                <ul>
                    <li>item 1</li>
                    <li>item 2</li>
                </ul>
                <ol>
                    <li>item 1</li>
                    <li>item 2</li>
                </ol>
            </body></html>");

        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        actual.ShouldMatch(new("body", null, [
            new("ul", null, 
            [
                new("li"),  
                new("li")
            ]),
            new("ol", null, 
            [
                new("li"),  
                new("li")
            ])
        ]));
    }

    private static async Task<IDocument> CreateHtmlDocument(string html)
    {
        var context = BrowsingContext.New(Configuration.Default.WithCss());
        var document = await context.OpenAsync(req => req.Content(html));
        return document;
    }

}
