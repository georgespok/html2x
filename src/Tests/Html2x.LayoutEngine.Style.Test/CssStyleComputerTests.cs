using AngleSharp;
using AngleSharp.Dom;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Style.Test.Assertions;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Style.Test;

public class CssStyleComputerTests
{
    private readonly CssStyleComputer _sut = new();

    [Fact]
    public async Task Compute_ProjectComputedStyles()
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
    public async Task Compute_LineHeightMultiplier_ParsesUnitlessValue()
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
    public async Task Compute_LineHeightNormal_DoesNotInheritParentMultiplier()
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

    [Theory]
    [InlineData("left", HtmlCssConstants.CssValues.Left)]
    [InlineData("right", HtmlCssConstants.CssValues.Right)]
    [InlineData("none", HtmlCssConstants.Defaults.FloatDirection)]
    public async Task Compute_WithFloatDeclaration_StoresComputedFloatDirection(
        string floatValue,
        string expectedDirection)
    {
        var document = await CreateHtmlDocument($@"
            <html>
              <body>
                <img style='float: {floatValue};' src='hero.png' />
              </body>
            </html>");

        var tree = _sut.Compute(document);

        var imageStyle = tree.Root!.Children.ShouldHaveSingleItem().Style;
        imageStyle.FloatDirection.ShouldBe(expectedDirection);
    }

    [Fact]
    public async Task Compute_AbsolutePosition_StoresComputedPosition()
    {
        var document = await CreateHtmlDocument(@"
            <html>
              <body>
                <div style='position: absolute;'>Box</div>
              </body>
            </html>");

        var tree = _sut.Compute(document);

        var blockStyle = tree.Root!.Children.ShouldHaveSingleItem().Style;
        blockStyle.Position.ShouldBe(HtmlCssConstants.CssValues.Absolute);
    }

    [Fact]
    public async Task Compute_NegativePadding_ClampsToZero()
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
    public async Task Compute_NestedBlocksAndInlines_ProducesExpectedTree()
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
    public async Task Compute_ListItems_ProducesExpectedTree()
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
    public async Task ParsePaddingShorthand_IndividualProperty_IndividualTakesPrecedence()
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
    public async Task Compute_UnsupportedWidthUnit_EmitsStyleDiagnostic()
    {
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div id='hero' style='width: 10rem;'>Box</div>
            </body></html>");
        var diagnostics = new DiagnosticsSession();

        _sut.Compute(document, diagnostics);

        var diagnostic = diagnostics.Events.Single(e => e.Name == "style/unsupported-declaration");
        diagnostic.Type.ShouldBe(DiagnosticsEventType.Warning);
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.Context.ShouldNotBeNull();
        diagnostic.Context.ElementIdentity.ShouldBe("div#hero");
        diagnostic.Context.StyleDeclaration.ShouldBe("width: 10rem");

        var payload = diagnostic.Payload.ShouldBeOfType<StyleDiagnosticPayload>();
        payload.PropertyName.ShouldBe("width");
        payload.RawValue.ShouldBe("10rem");
        payload.Decision.ShouldBe("Unsupported");
        payload.Reason.ShouldContain("Unsupported unit");
        payload.Context.ShouldNotBeNull();
        payload.Context.StructuralPath.ShouldBe("html/body/div#hero");
    }

    [Fact]
    public async Task Compute_InvalidSpacingShorthand_EmitsIgnoredStyleDiagnostic()
    {
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div id='box' style='padding: 1px 2px 3px 4px 5px;'>Box</div>
            </body></html>");
        var diagnostics = new DiagnosticsSession();

        _sut.Compute(document, diagnostics);

        var diagnostic = diagnostics.Events.Single(e => e.Name == "style/ignored-declaration");
        diagnostic.Type.ShouldBe(DiagnosticsEventType.Warning);
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);

        var payload = diagnostic.Payload.ShouldBeOfType<StyleDiagnosticPayload>();
        payload.PropertyName.ShouldBe("padding");
        payload.RawValue.ShouldBe("1px 2px 3px 4px 5px");
        payload.Decision.ShouldBe("Ignored");
        payload.Reason.ShouldContain("expected 1 to 4");
        payload.Context.ShouldNotBeNull();
        payload.Context.ElementIdentity.ShouldBe("div#box");
        payload.Context.StyleDeclaration.ShouldBe("padding: 1px 2px 3px 4px 5px");
    }

    [Fact]
    public async Task Compute_NegativePadding_EmitsPartiallyAppliedStyleDiagnostic()
    {
        var document = await CreateHtmlDocument(
            @"<html><body>
                <div class='summary' style='padding-top: -4px;'>Box</div>
            </body></html>");
        var diagnostics = new DiagnosticsSession();

        _sut.Compute(document, diagnostics);

        var diagnostic = diagnostics.Events.Single(e => e.Name == "style/partially-applied-declaration");
        diagnostic.Type.ShouldBe(DiagnosticsEventType.Warning);
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);

        var payload = diagnostic.Payload.ShouldBeOfType<StyleDiagnosticPayload>();
        payload.PropertyName.ShouldBe("padding-top");
        payload.RawValue.ShouldBe("-4px");
        payload.NormalizedValue.ShouldBe("0");
        payload.Decision.ShouldBe("PartiallyApplied");
        payload.Reason.ShouldContain("clamped to zero");
        payload.Context.ShouldNotBeNull();
        payload.Context.ElementIdentity.ShouldBe("div.summary");
        payload.Context.StyleDeclaration.ShouldBe("padding-top: -4px");
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
        var styleComputer = new CssStyleComputer();
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
