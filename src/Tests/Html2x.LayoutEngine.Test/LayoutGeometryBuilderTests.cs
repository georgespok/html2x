using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Test.Builders;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.LayoutEngine.Test;

public sealed class LayoutGeometryBuilderTests
{
    [Fact]
    public void Build_UsesStylePageMargins_AndPublishesBlocks()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(72, 10, 20, 30)
            .AddChild(HtmlCssConstants.HtmlTags.P, "Text", marginTop: 12, marginLeft: 4);

        var actual = Build(styles);

        actual.Page.Margin.ShouldBe(new Spacing(72f, 10f, 20f, 30f));
        var block = actual.Blocks.ShouldHaveSingleItem();
        block.Geometry.X.ShouldBe(34f);
        block.Geometry.Y.ShouldBe(84f);
    }

    [Fact]
    public void Build_DivAndText_PublishesInlineText()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssConstants.HtmlTags.Div, "Hello", marginTop: 15, marginLeft: 5);

        var actual = Build(styles);

        var block = actual.Blocks.ShouldHaveSingleItem();
        block.Identity.ElementIdentity.ShouldBe(HtmlCssConstants.HtmlTags.Div);
        block.Geometry.X.ShouldBe(5f);
        block.Geometry.Y.ShouldBe(15f);
        PublishedText(block).ShouldBe(["Hello"]);
    }

    [Fact]
    public void Build_DivAndBorder_PublishesVisualStyle()
    {
        var border = BorderEdges.Uniform(new BorderSide(0.75f, ColorRgba.Black, BorderLineStyle.Solid));
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssConstants.HtmlTags.Div, divNode => divNode
                .WithBorders(border)
                .AddText("Hello"));

        var actual = Build(styles);

        var block = actual.Blocks.ShouldHaveSingleItem();
        block.Style.Borders.ShouldBe(border);
        PublishedText(block).ShouldBe(["Hello"]);
    }

    [Fact]
    public void Build_ListItems_PublishesMarkerOffsetsAndInlineText()
    {
        var styles = BuildStyleTree()
            .AddChild(
                HtmlCssConstants.HtmlTags.Ul,
                ul => ul
                    .AddChild(HtmlCssConstants.HtmlTags.Li, "item 1")
                    .AddChild(HtmlCssConstants.HtmlTags.Li, "item 2"),
                marginTop: 15f,
                marginLeft: 5f);

        var actual = Build(styles);

        const string markerText = "\u2022 ";
        var list = actual.Blocks.ShouldHaveSingleItem();
        var items = list.Children;
        items.Count.ShouldBe(2);
        items[0].Display.Role.ShouldBe(FragmentDisplayRole.ListItem);
        items[0].Display.MarkerOffset.ShouldBe(HtmlCssConstants.Defaults.ListMarkerOffsetPt);
        PublishedText(items[0]).ShouldBe([markerText, "item 1"]);
        PublishedText(items[1]).ShouldBe([markerText, "item 2"]);
    }

    [Fact]
    public void Build_DivWithSpanAndParagraph_PublishesInlineAndBlockFlow()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssConstants.HtmlTags.Div, divNode => divNode
                .AddChild(HtmlCssConstants.HtmlTags.Span, "Span inside Div")
                .AddChild(HtmlCssConstants.HtmlTags.P, "Paragraph inside Div"));

        var actual = Build(styles);

        var div = actual.Blocks.ShouldHaveSingleItem();
        div.Flow.Select(static item => item.GetType()).ShouldBe(
        [
            typeof(PublishedInlineFlowSegmentItem),
            typeof(PublishedChildBlockItem)
        ]);
        PublishedText(div).ShouldBe(["Span inside Div"]);
        PublishedText(div.Children.ShouldHaveSingleItem()).ShouldBe(["Paragraph inside Div"]);
    }

    [Fact]
    public void Build_DivWithNestedDivInsideParagraph_PublishesNestedStructure()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssConstants.HtmlTags.Div, divNode => divNode
                .AddChild(HtmlCssConstants.HtmlTags.Span, "Span inside Div")
                .AddChild(HtmlCssConstants.HtmlTags.P, "Paragraph inside Div")
                .AddChild(HtmlCssConstants.HtmlTags.Div, nestedDivNode => nestedDivNode
                    .AddText("Nested Div inside Paragraph")
                    .AddChild(HtmlCssConstants.HtmlTags.Span, "Nested Span inside nested Div")));

        var actual = Build(styles);

        var div = actual.Blocks.ShouldHaveSingleItem();
        div.Children.Count.ShouldBe(2);
        PublishedText(div).ShouldBe(["Span inside Div"]);
        PublishedText(div.Children[0]).ShouldBe(["Paragraph inside Div"]);
        PublishedText(div.Children[1]).ShouldBe(["Nested Div inside Paragraph", "Nested Span inside nested Div"]);
    }

    [Fact]
    public void Build_BlockWithPadding_PublishesContentArea()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssConstants.HtmlTags.Div, divNode => divNode
                .WithPadding(15f, 11.25f, 7.5f, 3.75f)
                .AddText("Content"));

        var actual = Build(styles);

        var block = actual.Blocks.ShouldHaveSingleItem();
        block.Style.Padding.ShouldBe(new Spacing(15f, 11.25f, 7.5f, 3.75f));
        block.Geometry.ContentBoxRect.X.ShouldBe(block.Geometry.BorderBoxRect.X + 3.75f);
        block.Geometry.ContentBoxRect.Y.ShouldBe(block.Geometry.BorderBoxRect.Y + 15f);
    }

    private static StyleTreeBuilder BuildStyleTree() => new();

    private static PublishedLayoutTree Build(StyleTreeBuilder styles) =>
        new LayoutGeometryBuilder().Build(styles);

    private static IReadOnlyList<string> PublishedText(PublishedBlock block)
    {
        return block.Flow
            .OfType<PublishedInlineFlowSegmentItem>()
            .SelectMany(static item => item.Segment.Lines)
            .SelectMany(static line => line.Items)
            .OfType<PublishedInlineTextItem>()
            .SelectMany(static item => item.Runs)
            .Select(static run => run.Text)
            .ToList();
    }
}
