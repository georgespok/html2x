using AngleSharp;
using Html2x.Core;
using Html2x.Core.Layout;
using Html2x.Layout.Box;
using Html2x.Layout.Dom;
using Html2x.Layout.Fragment;
using Html2x.Layout.Style;
using Shouldly;
using CoreFragment = Html2x.Core.Layout.Fragment;

namespace Html2x.Layout.Test;

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

        var config = Configuration.Default.WithCss();
        var dom = new AngleSharpDomProvider(config);
        var style = new CssStyleComputer(new StyleTraversal(), new UserAgentDefaults());
        var boxes = new BoxTreeBuilder();
        var fragments = new FragmentBuilder();
        var builder = new LayoutBuilder(dom, style, boxes, fragments);

        // Act
        var layout = await builder.BuildAsync(html, PaperSizes.A4);

        // Assert
        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(2);

        var p = (BlockFragment)layout.Pages[0].Children[0];
        p.Rect.Y.ShouldBe(84f);
        p.Children.Count.ShouldBe(1);
        var div = (BlockFragment)layout.Pages[0].Children[1];
        div.Rect.Y.ShouldBe(112f, 0.5f);
        var lineBox = (LineBoxFragment)div.Children[0];
        lineBox.Runs[0].Text.ShouldBe("DIV");
    }

    [Fact]
    public async Task Build_WithNestedBlocksAndInlines_ProducesExpectedFragments()
    {
        const string html =
            "<html><body><div><span>Span inside Div</span><p>Paragraph inside Div<div>Nested Div inside Paragraph<span>Nested Span inside nested Div</span></div></p></div></body></html>";

        var config = Configuration.Default.WithCss();
        var domProvider = new AngleSharpDomProvider(config);
        var styleComputer = new CssStyleComputer(new StyleTraversal(), new UserAgentDefaults());
        var boxBuilder = new BoxTreeBuilder();
        var fragmentBuilder = new FragmentBuilder();
        var builder = new LayoutBuilder(domProvider, styleComputer, boxBuilder, fragmentBuilder);

        var document = await domProvider.LoadAsync(html);
        var styleTree = styleComputer.Compute(document);
        var boxTree = boxBuilder.Build(styleTree);
        var fragmentTree = fragmentBuilder.Build(boxTree);
        var layout = await builder.BuildAsync(html, PaperSizes.A4);

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

        var config = Configuration.Default.WithCss();
        var domProvider = new AngleSharpDomProvider(config);
        var styleComputer = new CssStyleComputer(new StyleTraversal(), new UserAgentDefaults());
        var boxBuilder = new BoxTreeBuilder();
        var fragmentBuilder = new FragmentBuilder();
        var builder = new LayoutBuilder(domProvider, styleComputer, boxBuilder, fragmentBuilder);

        var document = await domProvider.LoadAsync(html);
        var styleTree = styleComputer.Compute(document);
        var boxTree = boxBuilder.Build(styleTree);
        var fragmentTree = fragmentBuilder.Build(boxTree);
        var layout = await builder.BuildAsync(html, PaperSizes.Letter);

        var page = layout.Pages[0];
        var h1Block = (BlockFragment)page.Children[0];
        var h2Block = (BlockFragment)page.Children[1];
        var h3Block = (BlockFragment)page.Children[2];
        var h4Block = (BlockFragment)page.Children[3];
        var h5Block = (BlockFragment)page.Children[4];
        var h6Block = (BlockFragment)page.Children[5];

        // margin top = 5 + bottom = 5 , html -> pdf = 1.2
        const float rowGapPt = (5 + 5) * (float)1.2;

        h6Block.Rect.Y.ShouldBe(h1Block.Rect.Height +
                                h2Block.Rect.Height +
                                h3Block.Rect.Height +
                                h4Block.Rect.Height +
                                h5Block.Rect.Height + 5 * rowGapPt, 5);
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
}