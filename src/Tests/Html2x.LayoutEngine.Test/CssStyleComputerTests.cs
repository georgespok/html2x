using AngleSharp;
using AngleSharp.Dom;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Test.Assertions;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Test;

public class CssStyleComputerTests
{
    private readonly CssStyleComputer _sut = new(new StyleTraversal(), new CssValueConverter());

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
            new("p", new() { LineHeightMultiplier = 1.2f })
        ]));
    }

    [Fact]
    public async Task Compute_WithNegativePadding_ClampsToZero()
    {
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div style='padding: -20px;'>Text</div>
            </body></html>");

        var tree = _sut.Compute(document);
        var actual = StyleTreeSnapshot.FromTree(tree);

        actual.ShouldMatch(new("body", null, [
            new("div", new() { Padding = new Spacing(0f, 0f, 0f, 0f) })
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

    [Theory]
    [MemberData(nameof(BorderCases))]
    public async Task Compute_WithBorderStyles_ProducesExpectedTree(string html, BorderEdges expected)
    {
        var document = await CreateHtmlDocument(html);

        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                Borders = expected
            })
        ]));
    }

    [Theory]
    [MemberData(nameof(PaddingCases))]
    public async Task Compute_WithPaddingValues_ResolvesSpacing(string html, Spacing expected)
    {
        var document = await CreateHtmlDocument(html);

        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                Padding = expected
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
                Padding = new Spacing(15f, 11.25f, 7.5f, 3.75f)
            }, [
                new("p", new()
                {
                    Padding = new Spacing(0f, 0f, 0f, 0f)
                })
            ])
        ]));
    }

    [Theory]
    [InlineData("10px", 7.5f, 7.5f, 7.5f, 7.5f)]
    [InlineData("10px 20px", 7.5f, 15f, 7.5f, 15f)]
    [InlineData("10px 20px 15px", 7.5f, 15f, 11.25f, 15f)]
    [InlineData("10px 20px 15px 5px", 7.5f, 15f, 11.25f, 3.75f)]
    public async Task ParsePaddingShorthand_ResolvesToPoints(string shorthand, float top, float right, float bottom, float left)
    {
        // Arrange
        var document = await CreateHtmlDocument(
            $@"<html><body>
                <div style='padding: {shorthand};'>
                    Content
                </div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);

        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        actual.ShouldMatch(new("body", null, [
            new("div", new()
            {
                Padding = new Spacing(top, right, bottom, left)
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
                Padding = new Spacing(18.75f, 7.5f, 7.5f, 7.5f)
            })
        ]));
    }

    [Theory]
    [InlineData("100px", 75f)]
    [InlineData("50pt", 50f)]
    [InlineData("-10px", null)]
    [InlineData("none", null)]
    public async Task ParseMaxWidth_ResolvesCorrectly(string value, float? expectedPt)
    {
        // Arrange
        var document = await CreateHtmlDocument(
            $@"<html><body>
                <div style='max-width: {value};'>Box</div>
            </body></html>");

        // Act
        var tree = _sut.Compute(document);
        var actual = StyleTreeSnapshot.FromTree(tree);

        // Assert
        actual.ShouldMatch(new("body", null, [
            new("div", new() { MaxWidthPt = expectedPt })
        ]));
    }

    [Fact]
    public async Task Compute_UsesEmbeddedUserAgentStyleSheetByDefault()
    {
        const string html = "<html><body><h1>Title</h1><p>Text</p></body></html>";

        var options = new LayoutOptions
        {
            UseDefaultUserAgentStyleSheet = true
        };

        var actual = await ComputeStyleTreeAsync(html, options);

        actual.ShouldMatch(new StyleSnapshot("body", null,
        [
            new StyleSnapshot("h1", new ComputedStyle
            {
                FontSizePt = 18,
                Bold = true
            }),
            new StyleSnapshot("p", new ComputedStyle
            {
                Margin = new Spacing(6f, 0f, 6f, 0f)
            })
        ]));
    }

    [Fact]
    public async Task Compute_UserAgentStyleSheetOverride_ReplacesDefault()
    {
        const string html = "<html><body><h1>Title</h1><p>Text</p></body></html>";

        var options = new LayoutOptions
        {
            UseDefaultUserAgentStyleSheet = true,
            UserAgentStyleSheet = "h1 { font-size: 22pt; } p { margin: 2pt 0; }"
        };

        var actual = await ComputeStyleTreeAsync(html, options);

        actual.ShouldMatch(new StyleSnapshot("body", null,
        [
            new StyleSnapshot("h1", new ComputedStyle
            {
                FontSizePt = 22
            }),
            new StyleSnapshot("p", new ComputedStyle
            {
                Margin = new Spacing(2f, 0f, 2f, 0f)
            })
        ]));
    }

    [Fact]
    public async Task Compute_DisableDefaultUserAgentStyleSheet_RemovesDefaultMargins()
    {
        const string html = "<html><body><h1>Title</h1><p>Text</p></body></html>";

        var options = new LayoutOptions
        {
            UseDefaultUserAgentStyleSheet = false
        };

        var actual = await ComputeStyleTreeAsync(html, options);

        actual.ShouldMatch(new StyleSnapshot("body", null,
        [
            new StyleSnapshot("h1", new ComputedStyle
            {
                FontSizePt = 12
            }),
            new StyleSnapshot("p", new ComputedStyle
            {
                Margin = new Spacing(0f, 0f, 0f, 0f)
            })
        ]));
    }

    private static async Task<IDocument> CreateHtmlDocument(string html)
    {
        var context = BrowsingContext.New(Configuration.Default.WithCss());
        var document = await context.OpenAsync(req => req.Content(html));
        return document;
    }

    private static async Task<StyleSnapshot> ComputeStyleTreeAsync(string html, LayoutOptions options)
    {
        var config = Configuration.Default.WithCss();
        var domProvider = new AngleSharpDomProvider(config);
        var styleComputer = new CssStyleComputer(new StyleTraversal(), new CssValueConverter());
        var document = await domProvider.LoadAsync(html, options);
        var tree = styleComputer.Compute(document);
        return StyleTreeSnapshot.FromTree(tree);
    }

    public static IEnumerable<object[]> BorderCases()
    {
        var expected = BorderEdges.Uniform(new BorderSide(0.75f, ColorRgba.Black, BorderLineStyle.Dashed));

        yield return
        [
            @"<html><body>
                <div style='border-width: 1px; border-style: dashed;'>
                    Text                
                </div>
            </body></html>",
            expected
        ];

        yield return
        [
            @"<html><body>
                <div style='border: 1px dashed;'>
                    Text
                </div>
            </body></html>",
            expected
        ];
    }

    public static IEnumerable<object[]> PaddingCases()
    {
        yield return
        [
            @"<html><body>
                <div style='padding-top: 20px; padding-right: 15px; padding-bottom: 10px; padding-left: 5px;'>
                    Content
                </div>
            </body></html>",
            new Spacing(15f, 11.25f, 7.5f, 3.75f)
        ];

        yield return
        [
            @"<html><body>
                <div style='padding-top: 40px; padding-right: 30px; padding-bottom: 20px; padding-left: 10px;'>
                    Content
                </div>
            </body></html>",
            new Spacing(30f, 22.5f, 15f, 7.5f)
        ];

        yield return
        [
            @"<html><body>
                <div style='padding-top: 20px;'>
                    Content
                </div>
            </body></html>",
            new Spacing(15f, 0f, 0f, 0f)
        ];

        yield return
        [
            @"<html><body>
                <div>
                    Content without padding
                </div>
            </body></html>",
            new Spacing(0f, 0f, 0f, 0f)
        ];
    }

}
