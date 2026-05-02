using Html2x.RenderModel;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.Assertions;
using Html2x.LayoutEngine.Test.Builders;

namespace Html2x.LayoutEngine.Test;

public class BoxTreeBuilderTests
{
    [Fact]
    public void Build_UsesStylePageMargins_AndBuildsBlocks()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(72, 10, 20, 30)
            .AddChild(HtmlCssConstants.HtmlTags.P, "Text", marginTop: 12, marginLeft: 4);

        var actual = CreateBoxTreeBuilder().Build(styles);

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(72f, 10f, 20f, 30f))
            .Block(b => b.Position(30f + 4f, 72f + 12f)));
    }

    [Fact]
    public void Build_DivAndText_BuildsBlockAtExpectedPosition()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssConstants.HtmlTags.Div, "Hello", marginTop: 15, marginLeft: 5);

        var actual = CreateBoxTreeBuilder().Build(styles);

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(0f, 0f, 0f, 0f))
            .Block(b => b.Element(HtmlCssConstants.HtmlTags.Div)
                .Position(5f, 15f)
                .Inline(i => i.Text("Hello"))));
    }

    [Fact]
    public void Build_DivAndBorder_BuildsBlockAtExpectedPosition()
    {
        var border = BorderEdges.Uniform(new BorderSide(0.75f, ColorRgba.Black, BorderLineStyle.Solid));
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssConstants.HtmlTags.Div, divNode => divNode
                .WithBorders(border)
                .AddText("Hello"));

        var actual = CreateBoxTreeBuilder().Build(styles);

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(0f, 0f, 0f, 0f))
            .Block(b => b.Element(HtmlCssConstants.HtmlTags.Div)
                .Style(new ComputedStyle { Borders = border })
                .Inline(i => i.Text("Hello"))));
    }

    [Fact]
    public void Build_ListItems_BuildsBlockAtExpectedPosition()
    {
        var styles = BuildStyleTree()
            .AddChild(
                HtmlCssConstants.HtmlTags.Ul,
                ul => ul
                    .AddChild(HtmlCssConstants.HtmlTags.Li, "item 1")
                    .AddChild(HtmlCssConstants.HtmlTags.Li, "item 2"),
                marginTop: 15f,
                marginLeft: 5f);

        var actual = CreateBoxTreeBuilder().Build(styles);

        const string markerText = "\u2022 ";
        actual.ShouldMatch(tree => tree
            .Block(b =>
            {
                b.Element(HtmlCssConstants.HtmlTags.Ul)
                    .Block(firstLiBlock => firstLiBlock.Element(HtmlCssConstants.HtmlTags.Li)
                        .Inline(marker => marker.Text(markerText))
                        .Inline(text => text.Text("item 1")))
                    .Block(secondLiBlock => secondLiBlock.Element(HtmlCssConstants.HtmlTags.Li)
                        .Inline(marker => marker.Text(markerText))
                        .Inline(text => text.Text("item 2")));
            }));
    }

    [Fact]
    public void Build_DivWithSpanAndParagraph_HasInlineSpanAndBlockParagraph()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssConstants.HtmlTags.Div, divNode => divNode
                .AddChild(HtmlCssConstants.HtmlTags.Span, "Span inside Div")
                .AddChild(HtmlCssConstants.HtmlTags.P, "Paragraph inside Div"));

        var actual = CreateBoxTreeBuilder().Build(styles);

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(0f, 0f, 0f, 0f))
            .Block(b => b.Element(HtmlCssConstants.HtmlTags.Div)
                .Block(anon => anon.IsAnonymous(true)
                    .Inline(i => i.Element(HtmlCssConstants.HtmlTags.Span)
                        .Text("Span inside Div")))
                .Block(child => child.Element(HtmlCssConstants.HtmlTags.P)
                    .Inline(i => i.Text("Paragraph inside Div")))));
    }

    [Fact]
    public void Build_DivWithNestedDivInsideParagraph_BuildsNestedStructure()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssConstants.HtmlTags.Div, divNode => divNode
                .AddChild(HtmlCssConstants.HtmlTags.Span, "Span inside Div")
                .AddChild(HtmlCssConstants.HtmlTags.P, "Paragraph inside Div")
                .AddChild(HtmlCssConstants.HtmlTags.Div, nestedDivNode => nestedDivNode
                    .AddText("Nested Div inside Paragraph")
                    .AddChild(HtmlCssConstants.HtmlTags.Span, "Nested Span inside nested Div")));

        var actual = CreateBoxTreeBuilder().Build(styles);

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(0f, 0f, 0f, 0f))
            .Block(b => b.Element(HtmlCssConstants.HtmlTags.Div)
                .Block(anon => anon.IsAnonymous(true)
                    .Inline(i => i.Element(HtmlCssConstants.HtmlTags.Span)
                        .Text("Span inside Div")))
                .Block(paragraphBlock => paragraphBlock.Element(HtmlCssConstants.HtmlTags.P)
                    .Inline(i => i.Text("Paragraph inside Div")))
                .Block(nestedBlock => nestedBlock.Element(HtmlCssConstants.HtmlTags.Div)
                    .Inline(i => i.Text("Nested Div inside Paragraph"))
                    .Inline(i => i.Element(HtmlCssConstants.HtmlTags.Span)
                        .Inline(child => child.Text("Nested Span inside nested Div"))))));
    }

    [Fact]
    public void BlockBoxWithPadding_ReducesContentArea()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssConstants.HtmlTags.Div, divNode => divNode
                .WithPadding(15f, 11.25f, 7.5f, 3.75f)
                .AddText("Content"));

        var actual = CreateBoxTreeBuilder().Build(styles);

        actual.ShouldMatch(tree => tree
            .Page(p => p.Margins(0f, 0f, 0f, 0f))
            .Block(b => b.Element(HtmlCssConstants.HtmlTags.Div)
                .Padding(15f, 11.25f, 7.5f, 3.75f)));
    }

    private static StyleTreeBuilder BuildStyleTree() => new();

    private static BoxTreeBuilder CreateBoxTreeBuilder() => new();
}
