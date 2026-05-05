using Html2x.LayoutEngine.Contracts.Style;
using Shouldly;

namespace Html2x.LayoutEngine.Style.Test;

public sealed class StyleTreeContractTests
{
    [Fact]
    public void StyleNode_Element_StoresStyledElementFacts()
    {
        var node = new StyleNode
        {
            Element = StyledElementFacts.Create(HtmlCssConstants.HtmlTags.Div),
            Style = new ComputedStyle()
        };

        node.Element.ShouldBeOfType<StyledElementFacts>();
        node.Element.IsTag(HtmlCssConstants.HtmlTags.Div).ShouldBeTrue();
    }

    [Fact]
    public void StyleNode_Content_StoresOrderedContentNodes()
    {
        var child = new StyleNode
        {
            Element = StyledElementFacts.Create(HtmlCssConstants.HtmlTags.Span),
            Style = new ComputedStyle()
        };
        var node = new StyleNode(
            StyleSourceIdentity.Unspecified,
            StyledElementFacts.Create(HtmlCssConstants.HtmlTags.P),
            new ComputedStyle(),
            content:
            [
                StyleContentNode.ForText("alpha "),
                StyleContentNode.ForElement(child),
                StyleContentNode.ForText(" omega")
            ]);

        node.Content.Select(static item => item.Kind).ShouldBe([
            StyleContentNodeKind.Text,
            StyleContentNodeKind.Element,
            StyleContentNodeKind.Text
        ]);
    }

    [Fact]
    public void StyledElementFacts_AttributeLookup_IsCaseInsensitive()
    {
        var element = StyledElementFacts.Create(
            HtmlCssConstants.HtmlTags.Img,
            ("SRC", "hero.png"),
            ("Width", "200"));

        element.HasAttribute("src").ShouldBeTrue();
        element.HasAttribute("WIDTH").ShouldBeTrue();
        element.GetAttribute("src").ShouldBe("hero.png");
        element.GetAttribute("width").ShouldBe("200");
    }

    [Fact]
    public async Task StyleTree_CarriesLayoutAttributesWithoutDomTypes()
    {
        const string html = """
            <html><body>
              <img id='hero' class='lead art' src='hero.png' width='200' height='100' />
              <table><tr><td colspan='2' rowspan='3'>Cell</td></tr></table>
            </body></html>
            """;

        var tree = await new StyleTreeBuilder()
            .BuildAsync(html, new() { UseDefaultUserAgentStyleSheet = false });

        var imageNode = tree.Root!.Children[0];
        var image = imageNode.Element;
        imageNode.Identity.NodeId.IsSpecified.ShouldBeTrue();
        imageNode.Identity.ElementIdentity.ShouldBe("img#hero.lead.art");
        image.IsTag(HtmlCssConstants.HtmlTags.Img).ShouldBeTrue();
        image.Id.ShouldBe("hero");
        image.ClassAttribute.ShouldBe("lead art");
        image.GetAttribute(HtmlCssConstants.HtmlAttributes.Src).ShouldBe("hero.png");
        image.GetAttribute(HtmlCssConstants.HtmlAttributes.Width).ShouldBe("200");
        image.GetAttribute(HtmlCssConstants.HtmlAttributes.Height).ShouldBe("100");

        var cellNode = FindFirst(tree.Root, HtmlCssConstants.HtmlTags.Td);
        cellNode.Identity.NodeId.IsSpecified.ShouldBeTrue();
        cellNode.Identity.ElementIdentity.ShouldBe("td");
        var cell = cellNode.Element;
        cell.GetAttribute(HtmlCssConstants.HtmlAttributes.Colspan).ShouldBe("2");
        cell.GetAttribute(HtmlCssConstants.HtmlAttributes.Rowspan).ShouldBe("3");
    }

    [Fact]
    public void StyleContentNode_CanRepresentElementTextAndLineBreak()
    {
        var child = new StyleNode
        {
            Element = StyledElementFacts.Create(HtmlCssConstants.HtmlTags.Br),
            Style = new ComputedStyle()
        };

        var element = StyleContentNode.ForElement(child);
        var text = StyleContentNode.ForText("text");
        var lineBreak = StyleContentNode.LineBreak;

        element.Kind.ShouldBe(StyleContentNodeKind.Element);
        element.Element.ShouldBeSameAs(child);
        text.Kind.ShouldBe(StyleContentNodeKind.Text);
        text.Text.ShouldBe("text");
        lineBreak.Kind.ShouldBe(StyleContentNodeKind.LineBreak);
        lineBreak.Element.ShouldBeNull();
        lineBreak.Text.ShouldBeNull();
    }

    [Theory]
    [InlineData("node")]
    [InlineData("content")]
    public void StyleId_NegativeValue_ThrowsOutOfRange(string idKind)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            Action create = idKind switch
            {
                "node" => () => _ = new StyleNodeId(-1),
                "content" => () => _ = new StyleContentId(-1),
                _ => throw new ArgumentOutOfRangeException(nameof(idKind))
            };
            create();
        });
    }

    [Fact]
    public void StyleSourceIdentity_NegativeSourceOrder_ThrowsOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new StyleSourceIdentity(
            StyleNodeId.Unspecified,
            null,
            sourceOrder: -1,
            siblingIndex: 0,
            sourcePath: string.Empty,
            elementIdentity: null));
    }

    [Fact]
    public void StyleSourceIdentity_BlankElementIdentity_NormalizesToNull()
    {
        var identity = new StyleSourceIdentity(
            new StyleNodeId(1),
            null,
            sourceOrder: 1,
            siblingIndex: 0,
            sourcePath: "body[0]",
            elementIdentity: "  ");

        identity.ElementIdentity.ShouldBeNull();
    }

    [Fact]
    public void StyleContentIdentity_NegativeSiblingIndex_ThrowsOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new StyleContentIdentity(
            StyleContentId.Unspecified,
            StyleNodeId.Unspecified,
            sourceOrder: 0,
            siblingIndex: -1,
            sourcePath: string.Empty));
    }

    [Fact]
    public void StyleNode_DefaultIdentity_IsUnspecified()
    {
        var node = new StyleNode();

        node.Identity.ShouldBeSameAs(StyleSourceIdentity.Unspecified);
    }

    [Fact]
    public void StyleNode_Children_PublicContract_IsReadOnly()
    {
        var node = new StyleNode();

        var children = node.Children;

        children.ShouldBeEmpty();
    }

    [Fact]
    public void StyleNode_Content_PublicContract_IsReadOnly()
    {
        var node = new StyleNode();

        var content = node.Content;

        content.ShouldBeEmpty();
    }

    [Fact]
    public void StyleNode_Constructor_CopiesChildrenAndContent()
    {
        var child = new StyleNode
        {
            Element = StyledElementFacts.Create(HtmlCssConstants.HtmlTags.Span),
            Style = new ComputedStyle()
        };
        var children = new List<StyleNode> { child };
        var content = new List<StyleContentNode> { StyleContentNode.ForElement(child) };

        var node = new StyleNode(
            StyleSourceIdentity.Unspecified,
            StyledElementFacts.Create(HtmlCssConstants.HtmlTags.P),
            new ComputedStyle(),
            children,
            content);

        children.Add(new StyleNode());
        content.Add(StyleContentNode.ForText("late"));

        node.Children.Count.ShouldBe(1);
        node.Content.Count.ShouldBe(1);
    }

    [Fact]
    public void StyleContentNode_CompatibilityFactories_UseUnspecifiedIdentity()
    {
        var child = new StyleNode
        {
            Element = StyledElementFacts.Create(HtmlCssConstants.HtmlTags.Span),
            Style = new ComputedStyle()
        };

        var element = StyleContentNode.ForElement(child);
        var text = StyleContentNode.ForText("text");
        var lineBreak = StyleContentNode.LineBreak;

        element.Identity.ShouldBeSameAs(StyleContentIdentity.Unspecified);
        text.Identity.ShouldBeSameAs(StyleContentIdentity.Unspecified);
        lineBreak.Identity.ShouldBeSameAs(StyleContentIdentity.Unspecified);
    }

    [Fact]
    public void StyleContentNode_IdentityFactories_UseProvidedIdentity()
    {
        var identity = new StyleContentIdentity(
            new StyleContentId(2),
            new StyleNodeId(1),
            sourceOrder: 3,
            siblingIndex: 0,
            sourcePath: "body[0]/text[0]");

        var node = StyleContentNode.ForText(identity, "text");

        node.Identity.ShouldBeSameAs(identity);
        node.Kind.ShouldBe(StyleContentNodeKind.Text);
        node.Text.ShouldBe("text");
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
}
