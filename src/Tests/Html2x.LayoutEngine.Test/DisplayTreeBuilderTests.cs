using AngleSharp;
using AngleSharp.Dom;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Test;

public class DisplayTreeBuilderTests
{
    [Fact]
    public async Task Build_WithSingleDiv_ShouldCreateBlockWithInlineText()
    {
        const string html = "<html><body><div>Test</div></body></html>";
        var document = await ParseHtml(html);
        var div = document.QuerySelector("div")!;

        var styleTree = new StyleTree
        {
            Root = new StyleNode
            {
                Element = document.Body!,
                Style = new ComputedStyle(),
                Children =
                {
                    new StyleNode
                    {
                        Element = div,
                        Style = new ComputedStyle()
                    }
                }
            }
        };

        var builder = new DisplayTreeBuilder();

        var root = builder.Build(styleTree);

        var divBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        divBlock.Element.ShouldBe(div);
        divBlock.Children.Count.ShouldBe(1);

        var textInline = divBlock.Children[0].ShouldBeOfType<InlineBox>();
        textInline.TextContent.ShouldBe("Test");
    }

    [Fact]
    public async Task Build_WithListItem_ShouldCreateBlockWithInlineText()
    {
        const string html = "<html><body><ul><li>Test</li></ul></body></html>";
        var document = await ParseHtml(html);
        var ul = document.QuerySelector("ul")!;
        var li = document.QuerySelector("li")!;

        var styleTree = new StyleTree
        {
            Root = new StyleNode
            {
                Element = document.Body!,
                Style = new ComputedStyle(),
                Children =
                {
                    new StyleNode
                    {
                        Element = ul,
                        Style = new ComputedStyle(),
                        Children =
                        {
                            new StyleNode
                            {
                                Element = li,
                                Style = new ComputedStyle()
                            }
                        }
                    }
                }
            }
        };

        var builder = new DisplayTreeBuilder();

        var root = builder.Build(styleTree);

        var ulBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        ulBlock.Element.ShouldBe(ul);
        ulBlock.Children.Count.ShouldBe(1);

        var liBlock = ulBlock.Children[0].ShouldBeOfType<BlockBox>();
        liBlock.Element.ShouldBe(li);
        liBlock.Children.Count.ShouldBe(2);

        var markerInline = liBlock.Children[0].ShouldBeOfType<InlineBox>();
        markerInline.TextContent.ShouldBe("• ");

        var textInline = liBlock.Children[1].ShouldBeOfType<InlineBox>();
        textInline.TextContent.ShouldBe("Test");
    }

    [Fact]
    public async Task Build_WithBrAndIndentedText_ShouldCollapseIndent()
    {
        const string html =
            "<html><body><p>first line<br />second\r\n            line with spacing</p></body></html>";

        var document = await ParseHtml(html);
        var paragraph = document.QuerySelector("p")!;

        var styleTree = new StyleTree
        {
            Root = new StyleNode
            {
                Element = document.Body!,
                Style = new ComputedStyle(),
                Children =
                {
                    new StyleNode
                    {
                        Element = paragraph,
                        Style = new ComputedStyle()
                    }
                }
            }
        };

        var builder = new DisplayTreeBuilder();

        var root = builder.Build(styleTree);

        var paragraphBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        paragraphBlock.Children.Count.ShouldBe(2);

        var firstInline = paragraphBlock.Children[0].ShouldBeOfType<InlineBox>();
        firstInline.TextContent.ShouldBe("first line");

        var secondInline = paragraphBlock.Children[1].ShouldBeOfType<InlineBox>();
        secondInline.TextContent.ShouldBe("second line with spacing");
        secondInline.TextContent!.StartsWith(' ').ShouldBeFalse();
    }

    [Fact]
    public async Task Build_WithSeparatedTextNodes_ShouldKeepSingleSpaceBetweenRuns()
    {
        const string html = "<html><body><p>one <span> two</span></p></body></html>";
        var document = await ParseHtml(html);
        var paragraph = document.QuerySelector("p")!;
        var span = document.QuerySelector("span")!;

        var styleTree = new StyleTree
        {
            Root = new StyleNode
            {
                Element = document.Body!,
                Style = new ComputedStyle(),
                Children =
                {
                    new StyleNode
                    {
                        Element = paragraph,
                        Style = new ComputedStyle(),
                        Children =
                        {
                            new StyleNode
                            {
                                Element = span,
                                Style = new ComputedStyle()
                            }
                        }
                    }
                }
            }
        };

        var builder = new DisplayTreeBuilder();
        var root = builder.Build(styleTree);

        var paragraphBlock = root.Children[0].ShouldBeOfType<BlockBox>();

        var textRuns = new List<string>();
        CollectInlineText(paragraphBlock, textRuns);

        textRuns.Count.ShouldBe(2);
        textRuns[0].ShouldBe("one");
        textRuns[1].ShouldBe(" two");
        string.Join(string.Empty, textRuns).ShouldBe("one two");
    }

    [Fact]
    public async Task Build_WithMultipleSpacesInsideRun_ShouldCollapseToSingleSpace()
    {
        const string html = "<html><body><p>foo    bar</p></body></html>";
        var document = await ParseHtml(html);
        var paragraph = document.QuerySelector("p")!;

        var styleTree = new StyleTree
        {
            Root = new StyleNode
            {
                Element = document.Body!,
                Style = new ComputedStyle(),
                Children =
                {
                    new StyleNode
                    {
                        Element = paragraph,
                        Style = new ComputedStyle()
                    }
                }
            }
        };

        var builder = new DisplayTreeBuilder();
        var root = builder.Build(styleTree);

        var paragraphBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        var inline = paragraphBlock.Children[0].ShouldBeOfType<InlineBox>();
        inline.TextContent.ShouldBe("foo bar");
    }

    [Fact]
    public async Task Build_WithOrderedList_ShouldAssignIncrementingMarkers()
    {
        const string html = "<html><body><ol><li>First</li><li>Second</li><li>Third</li></ol></body></html>";
        var document = await ParseHtml(html);
        var ol = document.QuerySelector("ol")!;
        var liNodes = document.QuerySelectorAll("li");

        var styleTree = new StyleTree
        {
            Root = new StyleNode
            {
                Element = document.Body!,
                Style = new ComputedStyle(),
                Children =
                {
                    new StyleNode
                    {
                        Element = ol,
                        Style = new ComputedStyle(),
                        Children =
                        {
                            new StyleNode { Element = liNodes[0], Style = new ComputedStyle() },
                            new StyleNode { Element = liNodes[1], Style = new ComputedStyle() },
                            new StyleNode { Element = liNodes[2], Style = new ComputedStyle() }
                        }
                    }
                }
            }
        };

        var builder = new DisplayTreeBuilder();

        var root = builder.Build(styleTree);

        var olBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        olBlock.Element.ShouldBe(ol);
        olBlock.Children.Count.ShouldBe(3);

        olBlock.Children[0].Children[0].ShouldBeOfType<InlineBox>().TextContent.ShouldBe("1. ");
        olBlock.Children[0].Children[1].ShouldBeOfType<InlineBox>().TextContent.ShouldBe("First");

        olBlock.Children[1].Children[0].ShouldBeOfType<InlineBox>().TextContent.ShouldBe("2. ");
        olBlock.Children[1].Children[1].ShouldBeOfType<InlineBox>().TextContent.ShouldBe("Second");

        olBlock.Children[2].Children[0].ShouldBeOfType<InlineBox>().TextContent.ShouldBe("3. ");
        olBlock.Children[2].Children[1].ShouldBeOfType<InlineBox>().TextContent.ShouldBe("Third");
    }

    private static async Task<IDocument> ParseHtml(string html)
    {
        return await BrowsingContext.New(Configuration.Default)
            .OpenAsync(req => req.Content(html));
    }

    private static void CollectInlineText(DisplayNode node, IList<string> collector)
    {
        if (node is InlineBox inline && inline.TextContent is not null)
        {
            collector.Add(inline.TextContent);
        }

        foreach (var child in node.Children)
        {
            CollectInlineText(child, collector);
        }
    }
}
