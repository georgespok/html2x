using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Style.Test;

public sealed class StyleTreeBuilderTests
{
    [Fact]
    public async Task BuildAsync_RawHtml_ReturnsStyleTreeWithBodyRoot()
    {
        var tree = await BuildAsync("<html><body><p>Text</p></body></html>", DefaultOptions());

        tree.Root.ShouldNotBeNull();
        tree.Root.Element.IsTag(HtmlCssConstants.HtmlTags.Body).ShouldBeTrue();
        tree.Root.Children.ShouldHaveSingleItem().Element.IsTag(HtmlCssConstants.HtmlTags.P).ShouldBeTrue();
    }

    [Fact]
    public async Task BuildAsync_DefaultUserAgentStyleSheetEnabled_AppliesDefaults()
    {
        var tree = await BuildAsync(
            "<html><body><h1>Title</h1><p>Text</p></body></html>",
            new LayoutOptions { UseDefaultUserAgentStyleSheet = true });

        var heading = tree.Root!.Children[0];
        var paragraph = tree.Root.Children[1];

        heading.Style.FontSizePt.ShouldBe(18f);
        heading.Style.Bold.ShouldBeTrue();
        paragraph.Style.Margin.ShouldBe(new Spacing(6f, 0f, 6f, 0f));
    }

    [Fact]
    public async Task BuildAsync_UserAgentStyleSheetOverride_ReplacesDefault()
    {
        var tree = await BuildAsync(
            "<html><body><h1>Title</h1><p>Text</p></body></html>",
            new LayoutOptions
            {
                UseDefaultUserAgentStyleSheet = true,
                UserAgentStyleSheet = "h1 { font-size: 22pt; } p { margin: 2pt 0; }"
            });

        var heading = tree.Root!.Children[0];
        var paragraph = tree.Root.Children[1];

        heading.Style.FontSizePt.ShouldBe(22f);
        paragraph.Style.Margin.ShouldBe(new Spacing(2f, 0f, 2f, 0f));
    }

    [Fact]
    public async Task BuildAsync_DiagnosticsSession_EmitsStyleDiagnostic()
    {
        var diagnostics = new DiagnosticsSession();

        await BuildAsync(
            "<html><body><div id='hero' style='width: 10rem;'>Box</div></body></html>",
            DefaultOptions(),
            diagnostics);

        diagnostics.Events.ShouldContain(static e => e.Name == "stage/dom");
        diagnostics.Events.ShouldContain(static e => e.Name == "stage/style");
        diagnostics.Events.ShouldContain(static e => e.Name == "style/unsupported-declaration");

        var styleEvent = diagnostics.Events.Single(static e => e.Name == "style/unsupported-declaration");
        var payload = styleEvent.Payload.ShouldBeOfType<StyleDiagnosticPayload>();
        payload.PropertyName.ShouldBe(HtmlCssConstants.CssProperties.Width);
        payload.Context.ShouldNotBeNull();
        payload.Context.ElementIdentity.ShouldBe("div#hero");
    }

    [Fact]
    public async Task BuildAsync_MixedContent_PreservesOrderedTextAndElementContent()
    {
        var tree = await BuildAsync(
            "<html><body><p>alpha <span>beta</span> gamma</p></body></html>",
            DefaultOptions());

        var paragraph = tree.Root!.Children.ShouldHaveSingleItem();

        paragraph.Content.Count.ShouldBe(3);
        paragraph.Content[0].Kind.ShouldBe(StyleContentNodeKind.Text);
        paragraph.Content[0].Text.ShouldBe("alpha ");
        paragraph.Content[1].Kind.ShouldBe(StyleContentNodeKind.Element);
        paragraph.Content[1].Element.ShouldNotBeNull().Element.IsTag(HtmlCssConstants.HtmlTags.Span).ShouldBeTrue();
        paragraph.Content[2].Kind.ShouldBe(StyleContentNodeKind.Text);
        paragraph.Content[2].Text.ShouldBe(" gamma");
    }

    [Fact]
    public async Task BuildAsync_DuplicateSiblings_PreservesSiblingOrder()
    {
        var tree = await BuildAsync(
            """
            <html><body>
              <div class='item'>One</div>
              <div class='item'>Two</div>
              <div class='item'>Three</div>
            </body></html>
            """,
            DefaultOptions());

        var children = tree.Root!.Children;

        children.Count.ShouldBe(3);
        children.Select(static child => child.Element.TagName.ToLowerInvariant()).ShouldBe([
            HtmlCssConstants.HtmlTags.Div,
            HtmlCssConstants.HtmlTags.Div,
            HtmlCssConstants.HtmlTags.Div
        ]);
        children.Select(static child => child.Element.ClassAttribute).ShouldBe([
            "item",
            "item",
            "item"
        ]);
        children.Select(GetSingleTextContent).ShouldBe([
            "One",
            "Two",
            "Three"
        ]);
    }

    [Fact]
    public async Task BuildAsync_MixedContent_PreservesTextAroundNestedInlineChildren()
    {
        var tree = await BuildAsync(
            "<html><body><p>alpha <span>beta <b>bold</b></span> gamma</p></body></html>",
            DefaultOptions());

        var paragraph = tree.Root!.Children.ShouldHaveSingleItem();
        var span = paragraph.Content[1].Element.ShouldNotBeNull();

        paragraph.Content.Select(static content => content.Kind).ShouldBe([
            StyleContentNodeKind.Text,
            StyleContentNodeKind.Element,
            StyleContentNodeKind.Text
        ]);
        paragraph.Content[0].Text.ShouldBe("alpha ");
        paragraph.Content[2].Text.ShouldBe(" gamma");

        span.Element.IsTag(HtmlCssConstants.HtmlTags.Span).ShouldBeTrue();
        span.Content.Select(static content => content.Kind).ShouldBe([
            StyleContentNodeKind.Text,
            StyleContentNodeKind.Element
        ]);
        span.Content[0].Text.ShouldBe("beta ");
        span.Content[1].Element.ShouldNotBeNull().Element.IsTag(HtmlCssConstants.HtmlTags.B).ShouldBeTrue();
    }

    [Fact]
    public async Task BuildAsync_UnsupportedElement_PreservesFlattenedContentOrder()
    {
        var tree = await BuildAsync(
            "<html><body><p>alpha <custom>beta<br>gamma</custom> omega</p></body></html>",
            DefaultOptions());

        var paragraph = tree.Root!.Children.ShouldHaveSingleItem();

        paragraph.Children.Count.ShouldBe(0);
        paragraph.Content.Select(static content => content.Kind).ShouldBe([
            StyleContentNodeKind.Text,
            StyleContentNodeKind.Text,
            StyleContentNodeKind.LineBreak,
            StyleContentNodeKind.Text,
            StyleContentNodeKind.Text
        ]);
        paragraph.Content[0].Text.ShouldBe("alpha ");
        paragraph.Content[1].Text.ShouldBe("beta");
        paragraph.Content[2].Text.ShouldBeNull();
        paragraph.Content[2].Element.ShouldBeNull();
        paragraph.Content[3].Text.ShouldBe("gamma");
        paragraph.Content[4].Text.ShouldBe(" omega");
    }

    [Fact]
    public async Task BuildAsync_BodyRoot_AssignsSourceIdentity()
    {
        var tree = await BuildAsync("<html><body><p>Text</p></body></html>", DefaultOptions());

        var identity = tree.Root!.Identity;

        identity.NodeId.Value.ShouldBe(1);
        identity.NodeId.IsSpecified.ShouldBeTrue();
        identity.ParentId.ShouldBeNull();
        identity.SourceOrder.ShouldBe(1);
        identity.SiblingIndex.ShouldBe(0);
        identity.SourcePath.ShouldBe("body[0]");
        identity.ElementIdentity.ShouldBe("body");
    }

    [Fact]
    public async Task BuildAsync_NestedSupportedElements_AssignsParentAndSourceOrder()
    {
        var tree = await BuildAsync(
            "<html><body><div id='outer'><p><span class='note primary'>Text</span></p></div></body></html>",
            DefaultOptions());

        var root = tree.Root!;
        var outer = root.Children.ShouldHaveSingleItem();
        var paragraph = outer.Children.ShouldHaveSingleItem();
        var span = paragraph.Children.ShouldHaveSingleItem();

        AssertSourceIdentity(outer, root, 0, "body[0]/div[0]", "div#outer");
        AssertSourceIdentity(paragraph, outer, 0, "body[0]/div[0]/p[0]", "p");
        AssertSourceIdentity(span, paragraph, 0, "body[0]/div[0]/p[0]/span[0]", "span.note.primary");

        outer.Identity.SourceOrder.ShouldBeGreaterThan(root.Identity.SourceOrder);
        paragraph.Identity.SourceOrder.ShouldBeGreaterThan(outer.Identity.SourceOrder);
        span.Identity.SourceOrder.ShouldBeGreaterThan(paragraph.Identity.SourceOrder);
    }

    [Fact]
    public async Task BuildAsync_DuplicateSiblings_AssignsDistinctSiblingIndexes()
    {
        var tree = await BuildAsync(
            """
            <html><body>
              <div class='item'>One</div>
              <div class='item'>Two</div>
              <div class='item'>Three</div>
            </body></html>
            """,
            DefaultOptions());

        var children = tree.Root!.Children;

        children.Select(static child => child.Identity.NodeId.IsSpecified).ShouldBe([
            true,
            true,
            true
        ]);
        children.Select(static child => child.Identity.SiblingIndex).ShouldBe([0, 1, 2]);
        children.Select(static child => child.Identity.SourcePath).ShouldBe([
            "body[0]/div[0]",
            "body[0]/div[1]",
            "body[0]/div[2]"
        ]);
        children.Select(static child => child.Identity.ElementIdentity).ShouldBe([
            "div.item",
            "div.item",
            "div.item"
        ]);
        children.Select(static child => child.Identity.NodeId.Value).Distinct().Count().ShouldBe(3);
        children[0].Identity.SourceOrder.ShouldBeLessThan(children[1].Identity.SourceOrder);
        children[1].Identity.SourceOrder.ShouldBeLessThan(children[2].Identity.SourceOrder);
    }

    [Fact]
    public async Task BuildAsync_MixedContent_AssignsContentIdentities()
    {
        var tree = await BuildAsync(
            "<html><body><p>alpha <span>beta</span> gamma</p></body></html>",
            DefaultOptions());

        var paragraph = tree.Root!.Children.ShouldHaveSingleItem();
        var content = paragraph.Content;

        content.Select(static item => item.Identity.ContentId.IsSpecified).ShouldBe([
            true,
            true,
            true
        ]);
        content.Select(static item => item.Identity.ParentId).ShouldBe([
            paragraph.Identity.NodeId,
            paragraph.Identity.NodeId,
            paragraph.Identity.NodeId
        ]);
        content.Select(static item => item.Identity.SiblingIndex).ShouldBe([0, 1, 2]);
        content.Select(static item => item.Identity.SourcePath).ShouldBe([
            "body[0]/p[0]/text[0]",
            "body[0]/p[0]/element[1]",
            "body[0]/p[0]/text[2]"
        ]);
        content.Select(static item => item.Identity.ContentId.Value).Distinct().Count().ShouldBe(3);
        content[0].Identity.SourceOrder.ShouldBeLessThan(content[1].Identity.SourceOrder);
        content[1].Identity.SourceOrder.ShouldBeLessThan(content[2].Identity.SourceOrder);
        content[1].Element.ShouldNotBeNull().Element.IsTag(HtmlCssConstants.HtmlTags.Span).ShouldBeTrue();
    }

    [Fact]
    public async Task BuildAsync_LineBreak_AssignsContentIdentity()
    {
        var tree = await BuildAsync(
            "<html><body><p>alpha<br>omega</p></body></html>",
            DefaultOptions());

        var paragraph = tree.Root!.Children.ShouldHaveSingleItem();
        var lineBreak = paragraph.Content[1];

        lineBreak.Kind.ShouldBe(StyleContentNodeKind.Element);
        lineBreak.Identity.ContentId.IsSpecified.ShouldBeTrue();
        lineBreak.Identity.ParentId.ShouldBe(paragraph.Identity.NodeId);
        lineBreak.Identity.SiblingIndex.ShouldBe(1);
        lineBreak.Identity.SourcePath.ShouldBe("body[0]/p[0]/element[1]");
        lineBreak.Element.ShouldNotBeNull().Identity.ElementIdentity.ShouldBe("br");
    }

    [Fact]
    public async Task BuildAsync_UnsupportedFlattenedContent_AssignsContentIdentities()
    {
        var tree = await BuildAsync(
            "<html><body><p>alpha <custom>beta<br>gamma</custom> omega</p></body></html>",
            DefaultOptions());

        var paragraph = tree.Root!.Children.ShouldHaveSingleItem();
        var content = paragraph.Content;

        content.Select(static item => item.Identity.ContentId.IsSpecified).ShouldBe([
            true,
            true,
            true,
            true,
            true
        ]);
        content.Select(static item => item.Identity.ParentId).ShouldBe([
            paragraph.Identity.NodeId,
            paragraph.Identity.NodeId,
            paragraph.Identity.NodeId,
            paragraph.Identity.NodeId,
            paragraph.Identity.NodeId
        ]);
        content.Select(static item => item.Identity.SiblingIndex).ShouldBe([0, 1, 2, 3, 4]);
        content.Select(static item => item.Identity.SourcePath).ShouldBe([
            "body[0]/p[0]/text[0]",
            "body[0]/p[0]/text[1]",
            "body[0]/p[0]/line-break[2]",
            "body[0]/p[0]/text[3]",
            "body[0]/p[0]/text[4]"
        ]);
        content[2].Kind.ShouldBe(StyleContentNodeKind.LineBreak);
        content.Select(static item => item.Identity.ContentId.Value).Distinct().Count().ShouldBe(5);
    }

    private static Task<StyleTree> BuildAsync(
        string html,
        LayoutOptions options,
        DiagnosticsSession? diagnostics = null)
    {
        return new Html2x.LayoutEngine.Style.StyleTreeBuilder()
            .BuildAsync(html, options, diagnostics);
    }

    private static LayoutOptions DefaultOptions()
    {
        return new LayoutOptions
        {
            UseDefaultUserAgentStyleSheet = false
        };
    }

    private static string GetSingleTextContent(StyleNode node)
    {
        var content = node.Content.ShouldHaveSingleItem();
        content.Kind.ShouldBe(StyleContentNodeKind.Text);
        return content.Text.ShouldNotBeNull();
    }

    private static void AssertSourceIdentity(
        StyleNode node,
        StyleNode parent,
        int siblingIndex,
        string sourcePath,
        string elementIdentity)
    {
        node.Identity.NodeId.IsSpecified.ShouldBeTrue();
        node.Identity.ParentId.ShouldNotBeNull();
        node.Identity.ParentId.Value.ShouldBe(parent.Identity.NodeId);
        node.Identity.SiblingIndex.ShouldBe(siblingIndex);
        node.Identity.SourcePath.ShouldBe(sourcePath);
        node.Identity.ElementIdentity.ShouldBe(elementIdentity);
    }
}
