using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;
using Shouldly;
using CoreFragment = Html2x.RenderModel.Fragment;
using Html2x.Text;

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

        var layoutOptions = new LayoutBuildSettings
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

        var layoutOptions = new LayoutBuildSettings
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
    public async Task Build_NestedBlocksAndInlines_ProducesExpectedFragments()
    {
        const string html =
            "<html><body><div><span>Span inside Div</span><p>Paragraph inside Div<div>Nested Div inside Paragraph<span>Nested Span inside nested Div</span></div></p></div></body></html>";

        var styleTreeBuilder = new Html2x.LayoutEngine.Style.StyleTreeBuilder();
        var textMeasurer = CreateTextMeasurer();
        var layoutGeometryBuilder = new LayoutGeometryBuilder(textMeasurer);
        var fragmentBuilder = new FragmentBuilder();
        var builder = CreateLayoutBuilder();
        var layoutOptions = new LayoutBuildSettings
        {
            PageSize = PaperSizes.A4
        };

        var styleTree = await styleTreeBuilder.BuildAsync(html, layoutOptions.Style);
        var publishedLayout = layoutGeometryBuilder.Build(
            styleTree,
            new LayoutGeometryRequest
            {
                PageSize = layoutOptions.PageSize,
                ImageMetadataResolver = new NoopImageMetadataResolver(),
                HtmlDirectory = Directory.GetCurrentDirectory(),
                MaxImageSizeBytes = layoutOptions.MaxImageSizeBytes
            });
        var fragmentTree = fragmentBuilder.Build(publishedLayout);
        var layout = await builder.BuildAsync(html, layoutOptions);

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
    public async Task Build_H1H6_ProducesExpectedHeadingHeight()
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

        var layoutOptions = new LayoutBuildSettings
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
    public async Task Build_IndependentBorderSides_ProducesExpectedFragmentsAsync()
    {
        // Arrange: Block with border: 1px solid red and padding: 10px
        // Expected: Content width = 100px - 10px (left) - 10px (right) = 80px
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 200px; padding: 20px; border-left: 1px solid red; border-right: 1px solid blue; border-top: 1px solid green; border-bottom: 1px solid yellow;'>Content</div>
              </body>
            </html>";

        var layoutOptions = new LayoutBuildSettings
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
    public async Task Build_BorderAndPadding_OffsetsInlineContentByBorderPlusPadding()
    {
        // Arrange: padding 12px (9pt) and border 4px (3pt) should shift inline content by 12pt.
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='padding: 12px; border: 4px solid red;'>Hello</div>
              </body>
            </html>";

        var layoutOptions = new LayoutBuildSettings
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

        var layoutOptions = new LayoutBuildSettings
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
        image!.Rect.Width.ShouldBe(93f, 0.5f);
        image.Rect.Height.ShouldBe(78f, 0.5f);
        image.ContentRect.Width.ShouldBe(75f, 0.5f);
        image.ContentRect.Height.ShouldBe(60f, 0.5f);
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

        var layoutOptions = new LayoutBuildSettings
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
    public async Task Build_ImageSizing_AppliesCssRatioAndWidthCap()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 120pt; margin: 0;'>
                  <img src='css.png' style='display: block; width: 40px; height: 20px;' />
                  <img src='intrinsic.png' style='display: block;' />
                </div>
              </body>
            </html>";

        var layout = await CreateLayoutBuilder(new FixedImageMetadataResolver(src =>
            string.Equals(src, "intrinsic.png", StringComparison.OrdinalIgnoreCase)
                ? new SizePx(400d, 200d)
                : new SizePx(80d, 40d)))
            .BuildAsync(html, new LayoutBuildSettings { PageSize = PaperSizes.Letter });

        var container = layout.Pages[0].Children.ShouldHaveSingleItem().ShouldBeOfType<BlockFragment>();
        var images = EnumerateLayoutFragments(container)
            .OfType<ImageFragment>()
            .ToList();

        images.Count.ShouldBe(2);
        var cssImage = images[0];
        var intrinsicImage = images[1];

        cssImage.ContentRect.Width.ShouldBe(30f, 0.01f);
        cssImage.ContentRect.Height.ShouldBe(15f, 0.01f);
        intrinsicImage.ContentRect.Width.ShouldBe(120f, 0.01f);
        intrinsicImage.ContentRect.Height.ShouldBe(60f, 0.01f);
    }

    [Theory]
    [InlineData("margin: 0;", "width: 200px;", 150f)]
    [InlineData("margin: 200px;", "min-width: 400px;", 300f)]
    public async Task Build_WidthConstraint_ResolvesBlockWidth(
        string bodyStyle,
        string divStyle,
        float expectedWidth)
    {
        var html = $@"
            <html>
              <body style='{bodyStyle}'>
                <div style='{divStyle}'>Content</div>
              </body>
            </html>";

        var layoutOptions = new LayoutBuildSettings
        {
            PageSize = PaperSizes.A4
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var div = (BlockFragment)layout.Pages[0].Children[0];
        div.Rect.Width.ShouldBe(expectedWidth, 0.5f);
    }

    [Theory]
    [InlineData("height: 100px;", 75f)]
    [InlineData("min-height: 120px;", 90f)]
    [InlineData("height: 300px; max-height: 100px;", 75f)]
    public async Task Build_HeightConstraint_ResolvesBlockHeight(
        string divStyle,
        float expectedHeight)
    {
        var html = $@"
            <html>
              <body style='margin: 0;'>
                <div style='{divStyle}'>Hi</div>
              </body>
            </html>";

        var layoutOptions = new LayoutBuildSettings
        {
            PageSize = PaperSizes.A4
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var div = (BlockFragment)layout.Pages[0].Children[0];
        div.Rect.Height.ShouldBe(expectedHeight, 0.5f);
    }

    [Fact]
    public async Task Build_Br_SplitsInlineContentIntoSeparateLines()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <p>first line<br />second line</p>
              </body>
            </html>";

        var layoutOptions = new LayoutBuildSettings { PageSize = PaperSizes.Letter };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(1);

        var p = (BlockFragment)layout.Pages[0].Children[0];
        var lines = p.Children.OfType<LineBoxFragment>().ToList();
        lines.Count.ShouldBeGreaterThanOrEqualTo(2);

        lines[1].Rect.Y.ShouldBeGreaterThan(lines[0].Rect.Y);
    }

    [Fact]
    public async Task Build_LongDocument_FlowsBlocksAcrossMultiplePages()
    {
        var blocks = Enumerable.Range(1, 14)
            .Select(static i => $"<div style='height: 120px;'>Block {i}</div>");
        var html = $"<html><body style='margin: 0;'>{string.Join(string.Empty, blocks)}</body></html>";

        var layoutOptions = new LayoutBuildSettings
        {
            PageSize = PaperSizes.Letter
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBeGreaterThan(1);

        var renderedBlockLabels = layout.Pages
            .SelectMany(static page => page.Children.OfType<BlockFragment>())
            .SelectMany(static block => block.Children.OfType<LineBoxFragment>())
            .SelectMany(static line => line.Runs)
            .Select(static run => run.Text.Trim())
            .Where(static text => text.StartsWith("Block ", StringComparison.Ordinal))
            .ToList();

        renderedBlockLabels.Count.ShouldBe(14);
        renderedBlockLabels.ShouldBe(Enumerable.Range(1, 14).Select(static i => $"Block {i}").ToList());
    }

    [Fact]
    public async Task Build_SecondBlockDoesNotFit_StartsSecondBlockAtNextPageTop()
    {
        // Letter page size is 8.5in x 11in => 612pt x 792pt.
        // CSS pixel values are converted at 96dpi to points (1px = 0.75pt):
        // - 860px => 645pt
        // - 300px => 225pt
        // Combined flow height is 870pt, which must overflow a Letter content area
        // once top/bottom margins are applied, so block 2 must move to page 2.
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='height: 860px;'>Block 1</div>
                <div style='height: 300px;'>Block 2</div>
              </body>
            </html>";

        var layoutOptions = new LayoutBuildSettings
        {
            PageSize = PaperSizes.Letter
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBeGreaterThanOrEqualTo(2);
        layout.Pages[0].Children.Count.ShouldBe(1);
        layout.Pages[1].Children.Count.ShouldBe(1);

        var first = (BlockFragment)layout.Pages[0].Children[0];
        var second = (BlockFragment)layout.Pages[1].Children[0];
        // New-page-top rule: a moved block starts at that page's content top.
        // We derive this from the produced page margins instead of hard-coding
        // a margin value so the assertion stays valid if defaults change.
        var expectedTop = layout.Pages[1].Margins.Top;

        first.Children.OfType<LineBoxFragment>()
            .SelectMany(static line => line.Runs)
            .Any(static run => run.Text.Contains("Block 1", StringComparison.Ordinal))
            .ShouldBeTrue();

        second.Children.OfType<LineBoxFragment>()
            .SelectMany(static line => line.Runs)
            .Any(static run => run.Text.Contains("Block 2", StringComparison.Ordinal))
            .ShouldBeTrue();

        second.Rect.Y.ShouldBe(expectedTop, 0.5f);
    }

    
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

       var layoutOptions = new LayoutBuildSettings
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

        var layoutOptions = new LayoutBuildSettings
        {
            PageSize = PaperSizes.A4
        };

        // Act
        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        // Assert: Padding should affect horizontal spacing
        // For inline elements, padding affects the spacing around the content
        layout.Pages.Count.ShouldBe(1);
        var block = layout.Pages[0].Children.ShouldHaveSingleItem().ShouldBeOfType<BlockFragment>();
        var lineBox = block.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();
        lineBox.Runs.Count.ShouldBeGreaterThan(0);
        lineBox.Runs[0].Origin.X.ShouldBe(lineBox.Rect.X + 7.5f, 0.5f);
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

        var layoutOptions = new LayoutBuildSettings
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

        var layoutOptions = new LayoutBuildSettings
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
    public async Task Build_ParagraphContainsLineBreak_ReturnSingleBlockFragment()
    {
        // Arrange: Inline element with padding: 10px (7.5pt) inside a block
        const string html = @"
            <html>
              <p>first line<br/>second line</p>
            </html>";

        var layoutOptions = new LayoutBuildSettings
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
        block!.Children.OfType<LineBoxFragment>().Count().ShouldBe(2);
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

    private static IEnumerable<CoreFragment> EnumerateLayoutFragments(CoreFragment fragment)
    {
        yield return fragment;

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var nested in EnumerateLayoutFragments(child))
            {
                yield return nested;
            }
        }
    }

    private static ITextMeasurer CreateTextMeasurer()
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.Measure(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns((FontKey font, float _, string _) => TextMeasurement.CreateFallback(font, 10f, 9f, 3f));
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns(10f);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((9f, 3f));
        return textMeasurer.Object;
    }

    private static LayoutBuilder CreateLayoutBuilder()
    {
        return CreateLayoutBuilder(new NoopImageMetadataResolver());
    }

    private static LayoutBuilder CreateLayoutBuilder(IImageMetadataResolver imageMetadataResolver)
    {
        return new LayoutBuilder(CreateTextMeasurer(), imageMetadataResolver);
    }

    private sealed class FixedImageMetadataResolver(Func<string, SizePx> resolveSize) : IImageMetadataResolver
    {
        private readonly Func<string, SizePx> _resolveSize = resolveSize;

        public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes)
        {
            return new ImageMetadataResult
            {
                Src = src,
                Status = ImageMetadataStatus.Ok,
                IntrinsicSizePx = _resolveSize(src)
            };
        }
    }
}
