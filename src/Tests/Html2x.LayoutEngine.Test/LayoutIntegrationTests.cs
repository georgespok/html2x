using AngleSharp;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;
using Shouldly;
using CoreFragment = Html2x.Abstractions.Layout.Fragments.Fragment;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Test;

public class LayoutIntegrationTests
{
    [Fact]
    public async Task BodyMarginTop_IsAppliedToFirstBlockFragmentPosition()
    {
        // Arrange: 96px body margin-top (-> 72pt) + 12pt paragraph margin-top => expected first block Y = 84pt
        const string html = @"
            <html>
              <body style='margin: 96px;'>
                <p style='margin-top: 12pt;'>Paragraph</p>
                <div>DIV</div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        // Act
        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        // Assert
        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(2);

        var p = (BlockFragment)layout.Pages[0].Children[0];
        p.Rect.Y.ShouldBe(84f);
        p.Children.Count.ShouldBe(1);
        var div = (BlockFragment)layout.Pages[0].Children[1];
        div.Rect.Y.ShouldBe(104.4f, 0.5f);
        var lineBox = (LineBoxFragment)div.Children[0];
        lineBox.Runs[0].Text.ShouldBe("DIV");
    }

    [Fact]
    public async Task BlockMargins_CollapseBetweenAdjacentParagraphs()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <p style='margin: 0 0 20pt 0;'>First</p>
                <p style='margin: 10pt 0 0 0;'>Second</p>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(2);

        var first = (BlockFragment)layout.Pages[0].Children[0];
        var second = (BlockFragment)layout.Pages[0].Children[1];

        var expectedGap = 20f; // collapsed max(20pt, 10pt)
        var expectedSecondY = first.Rect.Y + first.Rect.Height + expectedGap;

        second.Rect.Y.ShouldBe(expectedSecondY, 0.5f);
    }

    [Fact]
    public async Task Build_WithNestedBlocksAndInlines_ProducesExpectedFragments()
    {
        const string html =
            "<html><body><div><span>Span inside Div</span><p>Paragraph inside Div<div>Nested Div inside Paragraph<span>Nested Span inside nested Div</span></div></p></div></body></html>";

        var config = Configuration.Default.WithCss();
        var domProvider = new AngleSharpDomProvider(config);
        var styleComputer = new CssStyleComputer(new StyleTraversal(), new CssValueConverter());
        var boxBuilder = new BoxTreeBuilder();
        var fragmentBuilder = new FragmentBuilder();
        var imageProvider = new NoopImageProvider();
        var builder = CreateLayoutBuilder(domProvider, styleComputer, boxBuilder, fragmentBuilder, imageProvider);
        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        var document = await domProvider.LoadAsync(html, layoutOptions);
        var styleTree = styleComputer.Compute(document);
        var boxTree = boxBuilder.Build(styleTree);
        var fragmentTree = fragmentBuilder.Build(boxTree, CreateContext(imageProvider));
        var layout = await builder.BuildAsync(html, layoutOptions);

        var boxTexts = CollectInlineTexts(boxTree.Blocks).ToList();
        boxTexts.ShouldContain("Span inside Div");
        boxTexts.ShouldContain("Paragraph inside Div");
        boxTexts.ShouldContain("Nested Div inside Paragraph");
        boxTexts.ShouldContain("Nested Span inside nested Div");

        var textRuns = CollectTextRuns(fragmentTree.Blocks).ToList();
        textRuns.ShouldContain("Span inside Div");
        textRuns.ShouldContain("Paragraph inside Div");
        textRuns.ShouldContain("Nested Div inside Paragraph");
        textRuns.ShouldContain("Nested Span inside nested Div");

        layout.Pages.Count.ShouldBe(1);
        var page = layout.Pages[0];
        page.Children.Count.ShouldBeGreaterThan(0);

        var outerDivBlock = page.Children.OfType<BlockFragment>().FirstOrDefault();
        outerDivBlock.ShouldNotBeNull();

        var nestedDivBlock = FindBlockContainingText(outerDivBlock!, "Nested Span inside nested Div");
        nestedDivBlock.ShouldNotBeNull();

        var paragraphBlock = FindBlockContainingText(outerDivBlock!, "Paragraph inside Div");
        paragraphBlock.ShouldNotBeNull();

        nestedDivBlock!.Rect.Y.ShouldBeGreaterThanOrEqualTo(paragraphBlock!.Rect.Y);
        paragraphBlock.Rect.Y.ShouldBeGreaterThanOrEqualTo(0f);
    }

    [Fact]
    public async Task Build_WithH1H6_ProducesExpectedHeadingHeight()
    {
        const string html = @"
            <html>             
                <h1>H1</h1>
                <h2>H2</h2>
                <h3>H3</h3>
                <h4>H4</h4>
                <h5>H5</h5>
                <h6>H6</h6>                
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.Letter
        };
        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        var page = layout.Pages[0];
        var h1Block = (BlockFragment)page.Children[0];
        var h2Block = (BlockFragment)page.Children[1];
        var h3Block = (BlockFragment)page.Children[2];
        var h4Block = (BlockFragment)page.Children[3];
        var h5Block = (BlockFragment)page.Children[4];
        var h6Block = (BlockFragment)page.Children[5];

        // Collapsed margins: max(5, 5) = 5pt between headings.
        const float rowGapPt = 5f;

        h6Block.Rect.Y.ShouldBe(h1Block.Rect.Y +
                                h1Block.Rect.Height +
                                h2Block.Rect.Height +
                                h3Block.Rect.Height +
                                h4Block.Rect.Height +
                                h5Block.Rect.Height + 5 * rowGapPt, 5);
    }

    [Fact]
    public async Task Build_WithIndependentBorderSides_ProducesExpectedFragmentsAsync()
    {
        // Arrange: Block with border: 1px solid red and padding: 10px
        // Expected: Content width = 100px - 10px (left) - 10px (right) = 80px
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 200px; padding: 20px; border-left: 1px solid red; border-right: 1px solid blue; border-top: 1px solid green; border-bottom: 1px solid yellow;'>Content</div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.Letter
        };

        // Act
        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);
        
        // Assert
        layout.Pages.Count.ShouldBe(1);
        var page = layout.Pages[0];
        page.Children.Count.ShouldBe(1);

        var div = (BlockFragment)page.Children[0];

        div.Style.Borders.ShouldNotBeNull();
        
        // Assert border widths (1px at 96 DPI is 0.75pt)
        div.Style.Borders.Left!.Width.ShouldBe(0.75f);
        div.Style.Borders.Right!.Width.ShouldBe(0.75f);
        div.Style.Borders.Top!.Width.ShouldBe(0.75f);
        div.Style.Borders.Bottom!.Width.ShouldBe(0.75f);

        // Assert border colors
        div.Style.Borders.Left.Color.ShouldBe(new ColorRgba(255, 0, 0, 255));
        div.Style.Borders.Right.Color.ShouldBe(new ColorRgba(0, 0, 255, 255));
        div.Style.Borders.Top.Color.ShouldBe(new ColorRgba(0, 128, 0, 255));
        div.Style.Borders.Bottom.Color.ShouldBe(new ColorRgba(255, 255, 0, 255));

        // Assert border styles
        div.Style.Borders.Left.LineStyle.ShouldBe(BorderLineStyle.Solid);
        div.Style.Borders.Right.LineStyle.ShouldBe(BorderLineStyle.Solid);
        div.Style.Borders.Top.LineStyle.ShouldBe(BorderLineStyle.Solid);
        div.Style.Borders.Bottom.LineStyle.ShouldBe(BorderLineStyle.Solid);
    }

    [Fact]
    public async Task Build_WithBorderAndPadding_OffsetsInlineContentByBorderPlusPadding()
    {
        // Arrange: padding 12px (9pt) and border 4px (3pt) should shift inline content by 12pt.
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='padding: 12px; border: 4px solid red;'>Hello</div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.Letter
        };

        // Act
        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        // Assert
        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(1);

        var div = (BlockFragment)layout.Pages[0].Children[0];
        div.Children.Count.ShouldBeGreaterThan(0);

        var lineBox = div.Children.OfType<LineBoxFragment>().First();
        (lineBox.Rect.X - div.Rect.X).ShouldBe(12f, 0.5f);
        (lineBox.Rect.Y - div.Rect.Y).ShouldBe(12f, 0.5f);

        lineBox.Runs.Count.ShouldBe(1);
        lineBox.Runs[0].Origin.X.ShouldBe(lineBox.Rect.X, 0.5f);
        lineBox.BaselineY.ShouldBe(lineBox.Runs[0].Origin.Y, 0.5f);
    }

    [Fact]
    public async Task Build_InlineImageWithBorderAndPadding_IncludesOuterSize()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <p>Before <img src='image.png' width='100' height='80' style='padding: 10px; border: 2px solid black;' /> After</p>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.Letter
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var page = layout.Pages[0];
        var image = page.Children
            .OfType<BlockFragment>()
            .Select(FindFirstImageFragment)
            .FirstOrDefault(fragment => fragment is not null);

        image.ShouldNotBeNull();
        image!.Rect.Width.ShouldBe(118f, 0.5f);
        image.Rect.Height.ShouldBe(98f, 0.5f);
        image.ContentRect.Width.ShouldBe(100f, 0.5f);
        image.ContentRect.Height.ShouldBe(80f, 0.5f);
    }

    [Fact]
    public async Task Build_ImageWithOversizedBorderAndPadding_ClampsContentRectToZero()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div>
                    <img src='image.png' width='0' height='0' style='padding: 20px; border: 10px solid black;' />
                </div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.Letter
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var page = layout.Pages[0];
        var image = page.Children
            .OfType<BlockFragment>()
            .Select(FindFirstImageFragment)
            .FirstOrDefault(fragment => fragment is not null);

        image.ShouldNotBeNull();
        image!.ContentRect.Width.ShouldBe(0f);
        image.ContentRect.Height.ShouldBe(0f);
    }

    [Fact]
    public async Task Build_WithFixedWidth_UsesSpecifiedWidth()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 200px;'>Content</div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var div = (BlockFragment)layout.Pages[0].Children[0];
        div.Rect.Width.ShouldBe(150f, 0.5f);
    }

    [Fact]
    public async Task Build_WithMinWidth_OverridesAvailableWidth()
    {
        const string html = @"
            <html>
              <body style='margin: 200px;'>
                <div style='min-width: 400px;'>Content</div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var div = (BlockFragment)layout.Pages[0].Children[0];
        div.Rect.Width.ShouldBe(300f, 0.5f);
    }

    [Fact]
    public async Task Build_WithFixedHeight_UsesSpecifiedHeight()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='height: 100px;'>Content</div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var div = (BlockFragment)layout.Pages[0].Children[0];
        div.Rect.Height.ShouldBe(75f, 0.5f);
    }

    [Fact]
    public async Task Build_WithMinHeight_ClampsContentHeight()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='min-height: 120px;'>Hi</div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var div = (BlockFragment)layout.Pages[0].Children[0];
        div.Rect.Height.ShouldBe(90f, 0.5f);
    }

    [Fact]
    public async Task Build_WithMaxHeight_ClampsSpecifiedHeight()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='height: 300px; max-height: 100px;'>Hi</div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var div = (BlockFragment)layout.Pages[0].Children[0];
        div.Rect.Height.ShouldBe(75f, 0.5f);
    }

    [Fact]
    public async Task Build_WithBr_SplitsInlineContentIntoSeparateLines()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <p>first line<br />second line</p>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions { PageSize = PaperSizes.Letter };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(1);

        var p = (BlockFragment)layout.Pages[0].Children[0];
        var lines = p.Children.OfType<LineBoxFragment>().ToList();
        lines.Count.ShouldBeGreaterThanOrEqualTo(2);

        lines[1].Rect.Y.ShouldBeGreaterThan(lines[0].Rect.Y);
    }

    
    //[Fact(Skip = "Disabled while width and height is not implemented")]
    [Fact]
    public async Task LayoutBlockWithPadding_AdjustsContentWidth()
    {
        // Arrange: Block with width: 200px (150pt) and padding: 20px (15pt each side)
        // Expected: Content width = 150pt - 15pt (left) - 15pt (right) = 120pt
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 200px; padding: 20px;'>Content</div>
              </body>
            </html>";

       var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        // Act
        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        // Assert
        layout.Pages.Count.ShouldBe(1);
        var div = (BlockFragment)layout.Pages[0].Children[0];
        var lineBox = (LineBoxFragment)div.Children[0];
        
        lineBox.Rect.X.ShouldBe(15f, 0.5f);
        lineBox.Rect.Y.ShouldBe(15f, 0.5f);
    }

    //[Fact(Skip = "Disabled while width and height is not implemented")]
    [Fact]
    public async Task LayoutInlineWithPadding_AffectsHorizontalSpacing()
    {
        // Arrange: Inline element with padding: 10px (7.5pt) inside a block
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div><span style='padding: 10px;'>Inline</span></div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        // Act
        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        // Assert: Padding should affect horizontal spacing
        // For inline elements, padding affects the spacing around the content
        layout.Pages.Count.ShouldBe(1);
        if (layout.Pages[0].Children.Count > 0)
        {
            var block = (BlockFragment)layout.Pages[0].Children[0];
            if (block.Children.Count > 0)
            {
                var lineBox = (LineBoxFragment)block.Children[0];
                // Verify padding is applied (check that text run has spacing)
                lineBox.Runs.Count.ShouldBeGreaterThan(0);
                // The inline element's padding should be reflected in the layout
                // Padding offsets the text position within the block
                // Text should be offset by left padding (7.5pt)
                lineBox.Runs[0].Origin.X.ShouldBe(lineBox.Rect.X + 7.5f, 0.5f);
            }
        }
    }

    [Fact]
    public async Task LayoutInlineWithMargin_AffectsHorizontalSpacing()
    {
        // Arrange: Inline element with margin: 10px (7.5pt) inside a block
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div><span style='margin: 10px;'>Inline</span></div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        // Act
        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        // Assert
        layout.Pages.Count.ShouldBe(1);
        var block = (BlockFragment)layout.Pages[0].Children[0];
        var lineBox = (LineBoxFragment)block.Children[0];
        lineBox.Runs.Count.ShouldBeGreaterThan(0);
        lineBox.Runs[0].Origin.X.ShouldBe(lineBox.Rect.X + 7.5f, 0.5f);
    }

    [Fact]
    public async Task LayoutInlinePadding_CanForceLineWrap()
    {
        // Arrange: padding should consume width and force a wrap.
        // Note: width is not enforced in layout yet; max-width drives the constraint.
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='max-width: 40px;'>
                    <span style='padding: 10px;'>A</span>
                    <span>B</span>
                </div>
              </body>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        // Act
        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        // Assert
        layout.Pages.Count.ShouldBe(1);
        var block = (BlockFragment)layout.Pages[0].Children[0];
        var lines = block.Children.OfType<LineBoxFragment>().ToList();
        lines.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Build_ShouldReturnBlockFragment()
    {
        // Arrange: Inline element with padding: 10px (7.5pt) inside a block
        const string html = @"
            <html>
              <p>first line<br/>second line</p>
            </html>";

        var layoutOptions = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        // Act
        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        // Assert
        layout.Pages.Count.ShouldBe(1);
        var page = layout.Pages[0];
        page.Children.Count.ShouldBe(1);
        var block = page.Children[0] as BlockFragment;
        block.ShouldNotBeNull();
    }

    private static IEnumerable<string> CollectTextRuns(IEnumerable<CoreFragment> fragments)
    {
        foreach (var fragment in fragments)
        {
            foreach (var text in CollectTextRuns(fragment))
            {
                yield return text;
            }
        }
    }

    private static IEnumerable<string> CollectTextRuns(CoreFragment fragment)
    {
        if (fragment is LineBoxFragment line)
        {
            foreach (var run in line.Runs)
            {
                var trimmed = run.Text.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    yield return trimmed;
                }
            }
        }

        if (fragment is BlockFragment block)
        {
            foreach (var child in block.Children)
            {
                foreach (var text in CollectTextRuns(child))
                {
                    yield return text;
                }
            }
        }
    }

    private static BlockFragment? FindBlockContainingText(BlockFragment block, string text)
    {
        if (block.Children.OfType<LineBoxFragment>()
            .Any(line => line.Runs.Any(run => run.Text.Contains(text, StringComparison.Ordinal))))
        {
            return block;
        }

        foreach (var childBlock in block.Children.OfType<BlockFragment>())
        {
            var match = FindBlockContainingText(childBlock, text);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static ImageFragment? FindFirstImageFragment(CoreFragment fragment)
    {
        if (fragment is ImageFragment image)
        {
            return image;
        }

        if (fragment is BlockFragment block)
        {
            foreach (var child in block.Children)
            {
                var match = FindFirstImageFragment(child);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> CollectInlineTexts(IEnumerable<DisplayNode> nodes)
    {
        foreach (var node in nodes)
        {
            foreach (var text in CollectInlineTexts(node))
            {
                yield return text;
            }
        }
    }

    private static IEnumerable<string> CollectInlineTexts(DisplayNode node)
    {
        if (node is InlineBox inline && !string.IsNullOrWhiteSpace(inline.TextContent))
        {
            yield return inline.TextContent.Trim();
        }

        foreach (var child in node.Children)
        {
            foreach (var text in CollectInlineTexts(child))
            {
                yield return text;
            }
        }
    }

    private static FragmentBuildContext CreateContext(IImageProvider imageProvider)
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns(10f);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((9f, 3f));

        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(x => x.Resolve(It.IsAny<FontKey>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));

        return new FragmentBuildContext(
            imageProvider,
            Directory.GetCurrentDirectory(),
            (long)(10 * 1024 * 1024),
            textMeasurer.Object,
            fontSource.Object);
    }

    private static LayoutBuilder CreateLayoutBuilder()
    {
        var config = Configuration.Default.WithCss();
        var dom = new AngleSharpDomProvider(config);
        var style = new CssStyleComputer(new StyleTraversal(), new CssValueConverter());
        var boxes = new BoxTreeBuilder();
        var fragments = new FragmentBuilder();
        var imageProvider = new NoopImageProvider();
        var builder = CreateLayoutBuilder(dom, style, boxes, fragments, imageProvider);
        return builder;
    }

    private static LayoutBuilder CreateLayoutBuilder(
        IDomProvider domProvider,
        IStyleComputer styleComputer,
        IBoxTreeBuilder boxBuilder,
        IFragmentBuilder fragmentBuilder,
        IImageProvider imageProvider)
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns(10f);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((9f, 3f));

        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(x => x.Resolve(It.IsAny<FontKey>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));

        return new LayoutBuilder(
            domProvider,
            styleComputer,
            boxBuilder,
            fragmentBuilder,
            imageProvider,
            textMeasurer.Object,
            fontSource.Object);
    }
}
