using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Style;
using Shouldly;

namespace Html2x.LayoutEngine.Test;

public class BoxTreeConstructionTests
{
    [Fact]
    public async Task Build_SingleDiv_CreateBlockWithInlineText()
    {
        const string html = "<html><body><div>Test</div></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var divBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        divBlock.Element!.IsTag(HtmlCssConstants.HtmlTags.Div).ShouldBeTrue();
        divBlock.Children.Count.ShouldBe(1);

        var textInline = divBlock.Children[0].ShouldBeOfType<InlineBox>();
        textInline.TextContent.ShouldBe("Test");
    }

    [Fact]
    public async Task Build_ListItem_CreateBlockWithInlineText()
    {
        const string html = "<html><body><ul><li>Test</li></ul></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var ulBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        ulBlock.Element!.IsTag(HtmlCssConstants.HtmlTags.Ul).ShouldBeTrue();
        ulBlock.Children.Count.ShouldBe(1);

        var liBlock = ulBlock.Children[0].ShouldBeOfType<BlockBox>();
        liBlock.Element!.IsTag(HtmlCssConstants.HtmlTags.Li).ShouldBeTrue();
        liBlock.Children.Count.ShouldBe(2);

        var markerInline = liBlock.Children[0].ShouldBeOfType<InlineBox>();
        markerInline.TextContent.ShouldBe("\u2022 ");

        var textInline = liBlock.Children[1].ShouldBeOfType<InlineBox>();
        textInline.TextContent.ShouldBe("Test");
    }

    [Fact]
    public async Task Build_BrAndIndentedText_CollapseIndent()
    {
        const string html =
            "<html><body><p>first line<br />second\r\n            line with spacing</p></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var paragraphBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        paragraphBlock.Children.Count.ShouldBe(3);

        var firstInline = paragraphBlock.Children[0].ShouldBeOfType<InlineBox>();
        firstInline.TextContent.ShouldBe("first line");

        var lineBreak = paragraphBlock.Children[1].ShouldBeOfType<InlineBox>();
        lineBreak.Element!.IsTag(HtmlCssConstants.HtmlTags.Br).ShouldBeTrue();
        lineBreak.SourceIdentity.NodeId.ShouldBe(
            FindFirst(styleTree.Root.ShouldNotBeNull(), HtmlCssConstants.HtmlTags.Br).Identity.NodeId);

        var secondInline = paragraphBlock.Children[2].ShouldBeOfType<InlineBox>();
        secondInline.TextContent.ShouldBe("second line with spacing");
        secondInline.TextContent!.StartsWith(' ').ShouldBeFalse();
    }

    [Fact]
    public async Task Build_SeparatedTextNodes_KeepSingleSpaceBetweenRuns()
    {
        const string html = "<html><body><p>one <span> two</span></p></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var paragraphBlock = root.Children[0].ShouldBeOfType<BlockBox>();

        var textRuns = new List<string>();
        CollectInlineText(paragraphBlock, textRuns);

        textRuns.Count.ShouldBe(2);
        textRuns[0].ShouldBe("one");
        textRuns[1].ShouldBe(" two");
        string.Join(string.Empty, textRuns).ShouldBe("one two");
    }

    [Fact]
    public async Task Build_MixedTextAndInlineElements_PreservesSourceOrder()
    {
        const string html = "<html><body><p>alpha <span>beta</span> gamma</p></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var textRuns = new List<string>();
        CollectInlineText(root.Children[0], textRuns);

        textRuns.ShouldBe(["alpha", " beta", " gamma"]);
        string.Join(string.Empty, textRuns).ShouldBe("alpha beta gamma");
    }

    [Fact]
    public async Task Build_UnsupportedWrapper_PreservesVisibleDescendantText()
    {
        const string html = "<html><body><p>alpha <em>beta <span>gamma</span></em> delta</p></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var textRuns = new List<string>();
        CollectInlineText(root.Children[0], textRuns);

        string.Join(string.Empty, textRuns).ShouldBe("alpha beta gamma delta");
    }

    [Fact]
    public async Task Build_MultipleSpacesInsideRun_CollapseToSingleSpace()
    {
        const string html = "<html><body><p>foo    bar</p></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var paragraphBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        var inline = paragraphBlock.Children[0].ShouldBeOfType<InlineBox>();
        inline.TextContent.ShouldBe("foo bar");
    }

    [Fact]
    public async Task Build_OrderedList_AssignIncrementingMarkers()
    {
        const string html = "<html><body><ol><li>First</li><li>Second</li><li>Third</li></ol></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var olBlock = root.Children[0].ShouldBeOfType<BlockBox>();
        olBlock.Element!.IsTag(HtmlCssConstants.HtmlTags.Ol).ShouldBeTrue();
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
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var tableBox = root.Children[0].ShouldBeOfType<TableBox>();
        var sectionBox = tableBox.Children.ShouldHaveSingleItem().ShouldBeOfType<TableSectionBox>();
        sectionBox.Element!.IsTag(HtmlCssConstants.HtmlTags.Tbody).ShouldBeTrue();
        sectionBox.Role.ShouldBe(BoxRole.TableSection);
        sectionBox.Children.ShouldHaveSingleItem()
            .ShouldBeOfType<TableRowBox>()
            .Element!.IsTag(HtmlCssConstants.HtmlTags.Tr).ShouldBeTrue();
    }

    [Fact]
    public async Task Build_CssFloatRight_CreateFloatBoxWithRightDirection()
    {
        const string html = "<html><body><img style='float: right;' src='hero.png' /></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var floatBox = root.Children.ShouldHaveSingleItem().ShouldBeOfType<FloatBox>();
        floatBox.FloatDirection.ShouldBe(HtmlCssConstants.CssValues.Right);
    }

    [Fact]
    public async Task Build_ImageElement_CarriesSourceAndSizeFacts()
    {
        const string html = "<html><body><img src='hero.png' width='200' height='100' /></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        var inlineImage = root.Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>();
        var imageBox = inlineImage.Children.ShouldHaveSingleItem().ShouldBeOfType<ImageBox>();
        imageBox.Src.ShouldBe("hero.png");
        imageBox.Element!.GetAttribute(HtmlCssConstants.HtmlAttributes.Width).ShouldBe("200");
        imageBox.Element.GetAttribute(HtmlCssConstants.HtmlAttributes.Height).ShouldBe("100");
    }

    [Fact]
    public async Task Build_HeroClassWithoutCssFloat_DoesNotCreateFloatBox()
    {
        const string html = "<html><body><img class='hero' src='hero.png' /></body></html>";
        var styleTree = await BuildStyleTree(html);

        var root = new BoxTreeConstruction().Build(styleTree);

        root.Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>();
    }

    [Fact]
    public async Task Build_SourceBackedBoxes_CopyStyleNodeIdentity()
    {
        const string html = "<html><body><div id='main' class='alpha'><span>Text</span></div></body></html>";
        var styleTree = await BuildStyleTree(html);
        var rootStyle = styleTree.Root.ShouldNotBeNull();
        var divStyle = FindFirst(rootStyle, HtmlCssConstants.HtmlTags.Div);
        var spanStyle = FindFirst(rootStyle, HtmlCssConstants.HtmlTags.Span);

        var root = new BoxTreeConstruction().Build(styleTree);

        var divBlock = root.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();
        var spanInline = divBlock.Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>();

        AssertSourceIdentity(divBlock.SourceIdentity, divStyle.Identity);
        AssertSourceIdentity(spanInline.SourceIdentity, spanStyle.Identity);
    }

    [Fact]
    public async Task Build_TextRuns_CopyContentIdentityAsGeneratedSourceIdentity()
    {
        const string html = "<html><body><p>alpha</p></body></html>";
        var styleTree = await BuildStyleTree(html);
        var paragraphStyle = FindFirst(styleTree.Root.ShouldNotBeNull(), HtmlCssConstants.HtmlTags.P);
        var textContent = paragraphStyle.Content.ShouldHaveSingleItem();
        textContent.Kind.ShouldBe(StyleContentNodeKind.Text);

        var root = new BoxTreeConstruction().Build(styleTree);

        var paragraphBlock = root.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();
        var textInline = paragraphBlock.Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>();

        textInline.TextContent.ShouldBe("alpha");
        textInline.SourceIdentity.NodeId.ShouldBe(textContent.Identity.ParentId);
        textInline.SourceIdentity.ContentId.ShouldBe(textContent.Identity.ContentId);
        textInline.SourceIdentity.SourcePath.ShouldBe(textContent.Identity.SourcePath);
        textInline.SourceIdentity.SourceOrder.ShouldBe(textContent.Identity.SourceOrder);
        textInline.SourceIdentity.ElementIdentity.ShouldBe(paragraphStyle.Identity.ElementIdentity);
        textInline.SourceIdentity.GeneratedKind.ShouldBe(GeometryGeneratedSourceKind.AnonymousText);
    }

    [Fact]
    public async Task Build_InlineBlockContentBox_GetsGeneratedSourceIdentity()
    {
        const string html = "<html><body><span style='display: inline-block;'>inside</span></body></html>";
        var styleTree = await BuildStyleTree(html);
        var spanStyle = FindFirst(styleTree.Root.ShouldNotBeNull(), HtmlCssConstants.HtmlTags.Span);

        var root = new BoxTreeConstruction().Build(styleTree);

        var inlineBlock = root.Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>();
        var contentBox = inlineBlock.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();

        contentBox.SourceIdentity.NodeId.ShouldBe(spanStyle.Identity.NodeId);
        contentBox.SourceIdentity.ContentId.ShouldBeNull();
        contentBox.SourceIdentity.SourcePath.ShouldBe($"{spanStyle.Identity.SourcePath}::inline-block-content");
        contentBox.SourceIdentity.ElementIdentity.ShouldBe(spanStyle.Identity.ElementIdentity);
        contentBox.SourceIdentity.GeneratedKind.ShouldBe(GeometryGeneratedSourceKind.InlineBlockContent);
    }

    [Fact]
    public async Task Build_ListMarker_GetsGeneratedSourceIdentity()
    {
        const string html = "<html><body><ul><li>item</li></ul></body></html>";
        var styleTree = await BuildStyleTree(html);
        var listItemStyle = FindFirst(styleTree.Root.ShouldNotBeNull(), HtmlCssConstants.HtmlTags.Li);

        var root = new BoxTreeConstruction().Build(styleTree);

        var list = root.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();
        var listItem = list.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();
        var marker = listItem.Children[0].ShouldBeOfType<InlineBox>();

        marker.TextContent.ShouldBe("\u2022 ");
        marker.SourceIdentity.NodeId.ShouldBe(listItemStyle.Identity.NodeId);
        marker.SourceIdentity.ContentId.ShouldBeNull();
        marker.SourceIdentity.SourcePath.ShouldBe($"{listItemStyle.Identity.SourcePath}::list-marker");
        marker.SourceIdentity.ElementIdentity.ShouldBe(listItemStyle.Identity.ElementIdentity);
        marker.SourceIdentity.GeneratedKind.ShouldBe(GeometryGeneratedSourceKind.ListMarker);
    }

    private static Task<StyleTree> BuildStyleTree(string html)
    {
        var builder = new StyleTreeBuilder();
        return builder.BuildAsync(
            html,
            new()
            {
                UseDefaultUserAgentStyleSheet = false
            });
    }

    private static StyleNode FindFirst(StyleNode node, string tagName)
    {
        if (node.Element.IsTag(tagName))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindFirstOrDefault(child, tagName);
            if (match is not null)
            {
                return match;
            }
        }

        throw new InvalidOperationException($"Could not find style node for tag '{tagName}'.");
    }

    private static StyleNode? FindFirstOrDefault(StyleNode node, string tagName)
    {
        if (node.Element.IsTag(tagName))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindFirstOrDefault(child, tagName);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
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

    private static void AssertSourceIdentity(
        GeometrySourceIdentity actual,
        StyleSourceIdentity expected)
    {
        actual.NodeId.ShouldBe(expected.NodeId);
        actual.ContentId.ShouldBeNull();
        actual.SourcePath.ShouldBe(expected.SourcePath);
        actual.ElementIdentity.ShouldBe(expected.ElementIdentity);
        actual.SourceOrder.ShouldBe(expected.SourceOrder);
        actual.GeneratedKind.ShouldBe(GeometryGeneratedSourceKind.None);
    }
}