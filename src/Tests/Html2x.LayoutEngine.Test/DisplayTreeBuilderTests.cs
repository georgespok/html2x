using AngleSharp;
using AngleSharp.Dom;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Style;
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

    private static async Task<IDocument> ParseHtml(string html)
    {
        return await BrowsingContext.New(Configuration.Default)
            .OpenAsync(req => req.Content(html));
    }
}
