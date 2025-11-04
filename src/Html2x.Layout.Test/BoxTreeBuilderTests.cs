using AngleSharp;
using AngleSharp.Dom;
using Html2x.Layout.Box;
using Html2x.Layout.Style;
using Html2x.Layout.Test.Assertions;

namespace Html2x.Layout.Test;

public class BoxTreeBuilderTests
{
    [Fact]
    public async Task Build_UsesStylePageMargins_AndBuildsBlocks()
    {
        // Arrange: simple DOM with <p> child so we get a block
        const string html = "<html><body><p>Text</p></body></html>";
        var doc = await ParseHtml(html);

        var p = doc.QuerySelector("p")!;

        var styles = BuildStyleTree(doc.Body!)
            .WithPageMargins(72, 10, 20, 30)
            .AddChild(p, 12, 4);

        // Act
        var actual = CreateBoxTreeBuilder().Build(styles);

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(72f, 10f, 20f, 30f))
            .Block(b => b.Position(30f + 4f, 72f + 12f)));
    }

    [Fact]
    public async Task Build_WithDivAndText_BuildsBlockAtExpectedPosition()
    {
        // Arrange: <div> with text; expect a block positioned by div margins
        const string html = "<html><body><div>Hello</div></body></html>";
        var doc = await ParseHtml(html);

        var div = doc.QuerySelector("div")!;

        var styles = BuildStyleTree(doc.Body!)
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(div, 15, 5);

        // Act
        var actual = CreateBoxTreeBuilder().Build(styles);

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(0f, 0f, 0f, 0f))
            .Block(b => b.Element(div)
                .Position(5f, 15f)
                .Inline(i => i.Text("Hello"))));
    }

    [Fact] 
    public async Task Build_WithListItems_BuildsBlockAtExpectedPosition()
    {
        const string html = "<html><ul><li>item 1</li><li>item 2</li></ul></html>";
        var doc = await ParseHtml(html);

        var ul = doc.QuerySelector("ul")!;
        var listItems = doc.QuerySelectorAll("li");
        var firstLi = listItems[0]!;
        var secondLi = listItems[1]!;

        var root = new StyleNode { Element = doc.Body!, Style = new ComputedStyle() };
        var ulNode = new StyleNode
        {
            Element = ul,
            Style = new ComputedStyle
            {
                MarginTopPt = 15f,
                MarginLeftPt = 5f
            }
        };
        ulNode.Children.Add(new StyleNode { Element = firstLi, Style = new ComputedStyle() });
        ulNode.Children.Add(new StyleNode { Element = secondLi, Style = new ComputedStyle() });
        root.Children.Add(ulNode);
        var styles = new StyleTree { Root = root };

        // Act
        var actual = CreateBoxTreeBuilder().Build(styles);

        const string markerChar = "•";

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(0f, 0f, 0f, 0f))
            .Block(b =>
            {
                b.Element(ul)
                    .Block(firstLiBlock => firstLiBlock.Element(firstLi)
                        .Inline(marker => marker.Text($"{markerChar} "))
                        .Inline(text => text.Text("item 1")))
                    .Block(secondLiBlock => secondLiBlock.Element(secondLi)
                        .Inline(marker => marker.Text($"{markerChar} "))
                        .Inline(text => text.Text("item 2")));
            }));
    }

    [Fact]
    public async Task Build_DivWithSpanAndParagraph_HasInlineSpanAndBlockParagraph()
    {
        // Arrange
        var doc = await ParseHtml(@"
            <html><body>
                <div>
                    <span>Span inside Div</span>
                    <p>Paragraph inside Div</p>
                </div>
            </body></html>");

        var div = doc.QuerySelector("div")!;
        var span = doc.QuerySelector("span")!;
        var p = doc.QuerySelector("p")!;

        var styles = BuildStyleTree(doc.Body!)
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(div, divNode => divNode
                .AddChild(span)
                .AddChild(p));

        // Act
        var actual = CreateBoxTreeBuilder().Build(styles);

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(0f, 0f, 0f, 0f))
            .Block(b => b.Element(div)
                .Inline(i => i.Element(span)
                    .Inline(child => child.Text("Span inside Div")))
                .Block(child => child.Element(p)
                    .Inline(i => i.Text("Paragraph inside Div")))));
    }

    [Fact]
    public async Task Build_DivWithNestedDivInsideParagraph_BuildsNestedStructure()
    {
        const string html =
            "<html><body><div><span>Span inside Div</span><p>Paragraph inside Div<div>Nested Div inside Paragraph<span>Nested Span inside nested Div</span></div></p></div></body></html>";
        var doc = await ParseHtml(html);

        var divs = doc.QuerySelectorAll("div");
        var outerDiv = divs[0]!;
        var nestedDiv = divs[1]!;
        var spans = doc.QuerySelectorAll("span");
        var outerSpan = spans[0]!;
        var nestedSpan = spans[1]!;
        var paragraph = doc.QuerySelector("p")!;

        var styles = BuildStyleTree(doc.Body!)
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(outerDiv, divNode => divNode
                .AddChild(outerSpan)
                .AddChild(paragraph, pNode => pNode
                    .AddChild(nestedDiv, nestedDivNode => nestedDivNode
                        .AddChild(nestedSpan))));

        var actual = CreateBoxTreeBuilder().Build(styles);

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(0f, 0f, 0f, 0f))
            .Block(b => b.Element(outerDiv)
                .Inline(i => i.Element(outerSpan)
                    .Inline(child => child.Text("Span inside Div")))
                .Block(paragraphBlock => paragraphBlock.Element(paragraph)
                    .Block(nestedBlock => nestedBlock.Element(nestedDiv)
                        .Inline(i => i.Element(nestedSpan)
                            .Inline(child => child.Text("Nested Span inside nested Div")))
                        .Inline(i => i.Text("Nested Div inside Paragraph")))
                    .Inline(i => i.Text("Paragraph inside Div")))));
    }

    private static BoxTreeBuilder CreateBoxTreeBuilder()
    {
        return new BoxTreeBuilder();
    }

    private static async Task<IDocument> ParseHtml(string html)
    {
        return await BrowsingContext
            .New(Configuration.Default)
            .OpenAsync(req => req.Content(html));
    }

    private static StyleTreeBuilder BuildStyleTree(IElement body)
    {
        return new StyleTreeBuilder(body);
    }

}
