using AngleSharp;
using AngleSharp.Dom;
using Html2x.Layout.Box;
using Html2x.Layout.Style;
using Shouldly;
using System.Linq;
using Html2x.Layout.Fragment;
using Html2x.Core.Layout;

namespace Html2x.Layout.Test;

public class BoxTreeBuilderTests
{
    [Fact]
    public async Task Build_UsesStylePageMargins_AndBuildsBlocks()
    {
        // Arrange: simple DOM with <p> child so we get a block
        const string html = "<html><body><p>Text</p></body></html>";
        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(html));

        IElement body = doc.Body!;
        var p = doc.QuerySelector("p")!;

        var styles = new StyleTree
        {
            Page =
            {
                MarginTopPt = 72, // 96px -> 72pt simulated
                MarginRightPt = 10,
                MarginBottomPt = 20,
                MarginLeftPt = 30
            },
            Root = new StyleNode
            {
                Element = body,
                Style = new ComputedStyle(),
            }
        };

        styles.Root!.Children.Add(new StyleNode
        {
            Element = p,
            Style = new ComputedStyle
            {
                MarginTopPt = 12,
                MarginLeftPt = 4,
                FontSizePt = 12
            }
        });

        var sut = new BoxTreeBuilder();

        // Act
        var boxTree = sut.Build(styles);

        // Assert page margins copied
        boxTree.Page.MarginTopPt.ShouldBe(72f);
        boxTree.Page.MarginRightPt.ShouldBe(10f);
        boxTree.Page.MarginBottomPt.ShouldBe(20f);
        boxTree.Page.MarginLeftPt.ShouldBe(30f);

        // Assert one block built and positioned from page margin + element margin
        boxTree.Blocks.Count.ShouldBe(1);
        var block = boxTree.Blocks[0];
        block.Y.ShouldBe(72f + 12f);
        block.X.ShouldBe(30f + 4f);
    }

    [Fact]
    public async Task Build_WithDivAndText_BuildsBlockAtExpectedPosition()
    {
        // Arrange: <div> with text; expect a block positioned by div margins
        const string html = "<html><body><div>Hello</div></body></html>";
        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(html));

        IElement body = doc.Body!;
        var div = doc.QuerySelector("div")!;

        var styles = new StyleTree
        {
            Page = { MarginTopPt = 0, MarginRightPt = 0, MarginBottomPt = 0, MarginLeftPt = 0 },
            Root = new StyleNode
            {
                Element = body,
                Style = new ComputedStyle(),
            }
        };

        styles.Root!.Children.Add(new StyleNode
        {
            Element = div,
            Style = new ComputedStyle
            {
                MarginTopPt = 15,
                MarginLeftPt = 5,
                FontSizePt = 12
            }
        });

        var sut = new BoxTreeBuilder();

        // Act
        var boxTree = sut.Build(styles);

        // Assert: one block built at div margins
        boxTree.Blocks.Count.ShouldBe(1);
        var block = boxTree.Blocks[0];
        block.Y.ShouldBe(15f);
        block.X.ShouldBe(5f);

        // And div contains an inline text node captured in the BoxTree
        var inline = block.Children.OfType<InlineBox>().Single();
        inline.TextContent.ShouldBe("Hello");
    }
}


