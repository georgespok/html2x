using AngleSharp;
using AngleSharp.Dom;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Test;

public class InitialBoxTreeBuilderTests
{
    [Fact]
    public async Task Build_SingleDiv_CreateBlockWithInlineText()
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

        var builder = new InitialBoxTreeBuilder();

        var root = builder.Build(styleTree);

        var divBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        divBlock.Element.ShouldBe(div);
        divBlock.Children.Count.ShouldBe(1);

        var textInline = divBlock.Children[0].ShouldBeOfType<InlineBox>();
        textInline.TextContent.ShouldBe("Test");
    }

    [Fact]
    public async Task Build_ListItem_CreateBlockWithInlineText()
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

        var builder = new InitialBoxTreeBuilder();

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
    public async Task Build_BrAndIndentedText_CollapseIndent()
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

        var builder = new InitialBoxTreeBuilder();

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
    public async Task Build_SeparatedTextNodes_KeepSingleSpaceBetweenRuns()
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

        var builder = new InitialBoxTreeBuilder();
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
    public async Task Build_MultipleSpacesInsideRun_CollapseToSingleSpace()
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

        var builder = new InitialBoxTreeBuilder();
        var root = builder.Build(styleTree);

        var paragraphBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        var inline = paragraphBlock.Children[0].ShouldBeOfType<InlineBox>();
        inline.TextContent.ShouldBe("foo bar");
    }

    [Fact]
    public async Task Build_OrderedList_AssignIncrementingMarkers()
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

        var builder = new InitialBoxTreeBuilder();

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

    [Fact]
    public async Task Build_TableSection_PreserveSectionRole()
    {
        const string html = "<html><body><table><tbody><tr><td>Cell</td></tr></tbody></table></body></html>";
        var document = await ParseHtml(html);
        var table = document.QuerySelector("table")!;
        var tbody = document.QuerySelector("tbody")!;
        var tr = document.QuerySelector("tr")!;
        var td = document.QuerySelector("td")!;

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
                        Element = table,
                        Style = new ComputedStyle(),
                        Children =
                        {
                            new StyleNode
                            {
                                Element = tbody,
                                Style = new ComputedStyle(),
                                Children =
                                {
                                    new StyleNode
                                    {
                                        Element = tr,
                                        Style = new ComputedStyle(),
                                        Children =
                                        {
                                            new StyleNode
                                            {
                                                Element = td,
                                                Style = new ComputedStyle()
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var builder = new InitialBoxTreeBuilder();

        var root = builder.Build(styleTree);

        var tableBox = root.Children[0].ShouldBeOfType<TableBox>();
        var sectionBox = tableBox.Children.ShouldHaveSingleItem().ShouldBeOfType<TableSectionBox>();
        sectionBox.Element.ShouldBe(tbody);
        sectionBox.Role.ShouldBe(BoxRole.TableSection);
        sectionBox.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>().Element.ShouldBe(tr);
    }

    [Fact]
    public async Task Build_CssFloatRight_CreateFloatBoxWithRightDirection()
    {
        const string html = "<html><body><img src='hero.png' /></body></html>";
        var document = await ParseHtml(html);
        var image = document.QuerySelector("img")!;
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
                        Element = image,
                        Style = new ComputedStyle { FloatDirection = HtmlCssConstants.CssValues.Right }
                    }
                }
            }
        };

        var root = new InitialBoxTreeBuilder().Build(styleTree);

        var floatBox = root.Children.ShouldHaveSingleItem().ShouldBeOfType<FloatBox>();
        floatBox.FloatDirection.ShouldBe(HtmlCssConstants.CssValues.Right);
    }

    [Fact]
    public async Task Build_HeroClassWithoutCssFloat_DoesNotCreateFloatBox()
    {
        const string html = "<html><body><img class='hero' src='hero.png' /></body></html>";
        var document = await ParseHtml(html);
        var image = document.QuerySelector("img")!;
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
                        Element = image,
                        Style = new ComputedStyle()
                    }
                }
            }
        };

        var root = new InitialBoxTreeBuilder().Build(styleTree);

        root.Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>();
    }

    private static async Task<IDocument> ParseHtml(string html)
    {
        return await BrowsingContext.New(Configuration.Default)
            .OpenAsync(req => req.Content(html));
    }

    private static void CollectInlineText(BoxNode node, IList<string> collector)
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
