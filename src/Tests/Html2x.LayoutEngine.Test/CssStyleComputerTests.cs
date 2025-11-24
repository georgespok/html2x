using AngleSharp;
using AngleSharp.Dom;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Test.Assertions;

namespace Html2x.LayoutEngine.Test;

public class CssStyleComputerTests
{
    private readonly CssStyleComputer _sut = new(new StyleTraversal(), new UserAgentDefaults());

    [Fact]
    public async Task Compute_ShouldProjectComputedStyles()
    {
        // Arrange

        var red = new ColorRgba(255, 0, 0, 255); // red #ff0000
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
            new() { FontFamily = fontHelvetica, FontSizePt = bodyFontSize, Color = red},
            [
                new ("h1", new()
                {
                    FontFamily = fontHelvetica, FontSizePt = 30, Color = red,
                    TextAlign = "right", Bold = true
                }),
                new ("p", new() { FontFamily = fontHelvetica, FontSizePt = bodyFontSize, Color = red }),
                new ("div", new() { FontFamily = fontHelvetica, FontSizePt = bodyFontSize, Color = red })
            ]));
    }

    [Fact]
    public async Task Compute_WithLineHeightMultiplier_ParsesUnitlessValue()
    {
        var document = await CreateHtmlDocument(@"
            <html>
              <body style='line-height: 1.4;'>
                <p>Paragraph</p>
              </body>
            </html>");

        var tree = _sut.Compute(document);
        var actual = StyleTreeSnapshot.FromTree(tree);

        actual.ShouldMatch(new("body", new() { LineHeightMultiplier = 1.4f }, [
            new("p", new() { LineHeightMultiplier = 1.4f })
        ]));
    }

    [Fact]
    public async Task Compute_WithLineHeightNormal_ShouldNotInheritParentMultiplier()
    {
        var document = await CreateHtmlDocument(@"
            <html>
              <body style='line-height: 1.8;'>
                <p style='line-height: normal;'>Paragraph</p>
              </body>
            </html>");

        var tree = _sut.Compute(document);
        var actual = StyleTreeSnapshot.FromTree(tree);

        actual.ShouldMatch(new("body", new() { LineHeightMultiplier = 1.8f }, [
            new("p", new() { LineHeightMultiplier = null })
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

    [Theory]
    [InlineData("blue", 0, 0, 255, 255)]
    [InlineData("rgba(255, 0, 0, 0.5)", 255, 0, 0, 127)]
    public async Task Compute_ColorVariants_ResolvesToRgba(string cssColor, byte r, byte g, byte b, byte a)
    {
        var document = await CreateHtmlDocument($@"
            <html><body>
                <p style='color: {cssColor};'>Text</p>
            </body></html>");

        var tree = _sut.Compute(document);
        var actual = StyleTreeSnapshot.FromTree(tree);

        actual.ShouldMatch(new("body", null, [
            new("p", new() { Color = new ColorRgba(r, g, b, a) })
        ]));
    }

    [Theory]
    [InlineData("#00FF00", 0, 255, 0, 255)]
    [InlineData("rgba(0, 0, 255, 0.25)", 0, 0, 255, 63)]
    [InlineData("yellow", 255, 255, 0, 255)]
    [InlineData("transparent", 0, 0, 0, 0)]
    public async Task Compute_BackgroundColor_ResolvesToRgba(string cssColor, byte r, byte g, byte b, byte a)
    {
        var document = await CreateHtmlDocument($@"
            <html><body>
                <div style='background-color: {cssColor};'>Box</div>
            </body></html>");

        var tree = _sut.Compute(document);
        var actual = StyleTreeSnapshot.FromTree(tree);

        actual.ShouldMatch(new("body", null, [
            new("div", new() { BackgroundColor = new ColorRgba(r, g, b, a) })
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

    [Fact]
    public async Task Compute_WithBorder_ProducesExpectedTree()
    {
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='border-width: 1px; border-style: dashed;'>
                    Text                
                </div>
            </body></html>");

        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                Borders = BorderEdges.Uniform(new BorderSide(0.75f, ColorRgba.Black, BorderLineStyle.Dashed))
            })
        ]));
    }

    [Fact]
    public async Task Compute_WithIndividualPaddingProperties_ParsesCorrectPointValues()
    {
        // Arrange
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='padding-top: 20px; padding-right: 15px; padding-bottom: 10px; padding-left: 5px;'>
                    Content
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        // Conversion: 1px = 0.75pt
        // padding-top: 20px = 15pt (20 * 0.75)
        // padding-right: 15px = 11.25pt (15 * 0.75)
        // padding-bottom: 10px = 7.5pt (10 * 0.75)
        // padding-left: 5px = 3.75pt (5 * 0.75)
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                PaddingTopPt = 15f,
                PaddingRightPt = 11.25f,
                PaddingBottomPt = 7.5f,
                PaddingLeftPt = 3.75f
            })
        ]));
    }

    [Fact]
    public async Task ParseIndividualPaddingProperties_WithAllSides_ReturnsCorrectPointValues()
    {
        // Arrange
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='padding-top: 40px; padding-right: 30px; padding-bottom: 20px; padding-left: 10px;'>
                    Content
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        // Conversion: 1px = 0.75pt
        // padding-top: 40px = 30pt (40 * 0.75)
        // padding-right: 30px = 22.5pt (30 * 0.75)
        // padding-bottom: 20px = 15pt (20 * 0.75)
        // padding-left: 10px = 7.5pt (10 * 0.75)
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                PaddingTopPt = 30f,
                PaddingRightPt = 22.5f,
                PaddingBottomPt = 15f,
                PaddingLeftPt = 7.5f
            })
        ]));
    }

    [Fact]
    public async Task ParsePaddingTop_WithPxValue_ConvertsToPoints()
    {
        // Arrange
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='padding-top: 20px;'>
                    Content
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        // Conversion: 1px = 0.75pt
        // padding-top: 20px = 15pt (20 * 0.75)
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                PaddingTopPt = 15f
            })
        ]));
    }

    [Fact]
    public async Task ParsePaddingProperties_WhenNotSpecified_DefaultsToZero()
    {
        // Arrange
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div>
                    Content without padding
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        // Padding should default to 0 for all sides when not specified
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                PaddingTopPt = 0f,
                PaddingRightPt = 0f,
                PaddingBottomPt = 0f,
                PaddingLeftPt = 0f
            })
        ]));
    }

    [Fact]
    public async Task ParsePaddingProperties_DoesNotInheritFromParent()
    {
        // Arrange
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='padding-top: 20px; padding-right: 15px; padding-bottom: 10px; padding-left: 5px;'>
                    <p>
                        Child without padding
                    </p>
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        // Parent has padding, child should have 0 (padding does not inherit)
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                PaddingTopPt = 15f,
                PaddingRightPt = 11.25f,
                PaddingBottomPt = 7.5f,
                PaddingLeftPt = 3.75f
            }, [
                new("p", new()
                {
                    PaddingTopPt = 0f,
                    PaddingRightPt = 0f,
                    PaddingBottomPt = 0f,
                    PaddingLeftPt = 0f
                })
            ])
        ]));
    }

    [Fact]
    public async Task ParsePaddingShorthand_SingleValue_SetsAllSides()
    {
        // Arrange
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='padding: 10px;'>
                    Content
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        // Conversion: 1px = 0.75pt
        // padding: 10px should set all sides to 7.5pt (10 * 0.75)
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                PaddingTopPt = 7.5f,
                PaddingRightPt = 7.5f,
                PaddingBottomPt = 7.5f,
                PaddingLeftPt = 7.5f
            })
        ]));
    }

    [Fact]
    public async Task ParsePaddingShorthand_TwoValues_SetsVerticalAndHorizontal()
    {
        // Arrange
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='padding: 10px 20px;'>
                    Content
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        // Conversion: 1px = 0.75pt
        // padding: 10px 20px → top/bottom=7.5pt (10 * 0.75), left/right=15pt (20 * 0.75)
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                PaddingTopPt = 7.5f,
                PaddingRightPt = 15f,
                PaddingBottomPt = 7.5f,
                PaddingLeftPt = 15f
            })
        ]));
    }

    [Fact]
    public async Task ParsePaddingShorthand_ThreeValues_SetsTopHorizontalBottom()
    {
        // Arrange
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='padding: 10px 20px 15px;'>
                    Content
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        // Conversion: 1px = 0.75pt
        // padding: 10px 20px 15px → top=7.5pt (10 * 0.75), left/right=15pt (20 * 0.75), bottom=11.25pt (15 * 0.75)
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                PaddingTopPt = 7.5f,
                PaddingRightPt = 15f,
                PaddingBottomPt = 11.25f,
                PaddingLeftPt = 15f
            })
        ]));
    }

    [Fact]
    public async Task ParsePaddingShorthand_FourValues_SetsAllSidesIndividually()
    {
        // Arrange
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='padding: 10px 20px 15px 5px;'>
                    Content
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        // Conversion: 1px = 0.75pt
        // padding: 10px 20px 15px 5px → top=7.5pt (10 * 0.75), right=15pt (20 * 0.75), bottom=11.25pt (15 * 0.75), left=3.75pt (5 * 0.75)
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                PaddingTopPt = 7.5f,
                PaddingRightPt = 15f,
                PaddingBottomPt = 11.25f,
                PaddingLeftPt = 3.75f
            })
        ]));
    }

    [Fact]
    public async Task ParsePaddingShorthand_WithIndividualProperty_IndividualTakesPrecedence()
    {
        // Arrange
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='padding: 10px; padding-top: 25px;'>
                    Content
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        // Conversion: 1px = 0.75pt
        // padding: 10px sets all sides to 7.5pt, but padding-top: 25px overrides top to 18.75pt (25 * 0.75)
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                PaddingTopPt = 18.75f,
                PaddingRightPt = 7.5f,
                PaddingBottomPt = 7.5f,
                PaddingLeftPt = 7.5f
            })
        ]));
    }

    private static async Task<IDocument> CreateHtmlDocument(string html)
    {
        var context = BrowsingContext.New(Configuration.Default.WithCss());
        var document = await context.OpenAsync(req => req.Content(html));
        return document;
    }

}
