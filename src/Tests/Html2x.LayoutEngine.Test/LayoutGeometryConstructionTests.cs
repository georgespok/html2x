using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Stage.Contracts.Geometry;
using Html2x.LayoutEngine.Test.Builders;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.LayoutEngine.Test;

public sealed class LayoutGeometryConstructionTests
{
    [Fact]
    public void Build_UsesStylePageMargins_AndPublishesBlocks()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(72, 10, 20, 30)
            .AddChild(HtmlCssVocabulary.HtmlTags.P, "Text", 12, 4);

        var actual = Build(styles);

        actual.Page.Margin.ShouldBe(new(72f, 10f, 20f, 30f));
        var block = actual.Blocks.ShouldHaveSingleItem();
        block.Geometry.X.ShouldBe(34f);
        block.Geometry.Y.ShouldBe(84f);
    }

    [Fact]
    public void Build_DivAndText_PublishesInlineText()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssVocabulary.HtmlTags.Div, "Hello", 15, 5);

        var actual = Build(styles);

        var block = actual.Blocks.ShouldHaveSingleItem();
        block.Identity.ElementIdentity.ShouldBe(HtmlCssVocabulary.HtmlTags.Div);
        block.Geometry.X.ShouldBe(5f);
        block.Geometry.Y.ShouldBe(15f);
        PublishedText(block).ShouldBe(["Hello"]);
    }

    [Fact]
    public void Build_ThroughStageContract_PublishesLayout()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssVocabulary.HtmlTags.Div, "Hello", 15, 5);
        ILayoutGeometryStage stage = new LayoutGeometryConstruction(new ConstantTextMeasurer(10f, 9f, 3f));

        var actual = stage.Build(new(styles, LayoutGeometryRequest.Default, null));

        var block = actual.Blocks.ShouldHaveSingleItem();
        block.Identity.ElementIdentity.ShouldBe(HtmlCssVocabulary.HtmlTags.Div);
        PublishedText(block).ShouldBe(["Hello"]);
    }

    [Fact]
    public void Build_DivAndBorder_PublishesVisualStyle()
    {
        var border = BorderEdges.Uniform(new(0.75f, ColorRgba.Black, BorderLineStyle.Solid));
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssVocabulary.HtmlTags.Div, divNode => divNode
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
                HtmlCssVocabulary.HtmlTags.Ul,
                ul => ul
                    .AddChild(HtmlCssVocabulary.HtmlTags.Li, "item 1")
                    .AddChild(HtmlCssVocabulary.HtmlTags.Li, "item 2"),
                15f,
                5f);

        var actual = Build(styles);

        const string markerText = "\u2022 ";
        var list = actual.Blocks.ShouldHaveSingleItem();
        var items = list.Children;
        items.Count.ShouldBe(2);
        items[0].Display.Role.ShouldBe(FragmentDisplayRole.ListItem);
        items[0].Display.MarkerOffset.ShouldBe(HtmlCssVocabulary.Defaults.ListMarkerOffsetPt);
        PublishedText(items[0]).ShouldBe([markerText, "item 1"]);
        PublishedText(items[1]).ShouldBe([markerText, "item 2"]);
    }

    [Fact]
    public void Build_DivWithSpanAndParagraph_PublishesInlineAndBlockFlow()
    {
        var styles = BuildStyleTree()
            .WithPageMargins(0, 0, 0, 0)
            .AddChild(HtmlCssVocabulary.HtmlTags.Div, divNode => divNode
                .AddChild(HtmlCssVocabulary.HtmlTags.Span, "Span inside Div")
                .AddChild(HtmlCssVocabulary.HtmlTags.P, "Paragraph inside Div"));

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
            .AddChild(HtmlCssVocabulary.HtmlTags.Div, divNode => divNode
                .AddChild(HtmlCssVocabulary.HtmlTags.Span, "Span inside Div")
                .AddChild(HtmlCssVocabulary.HtmlTags.P, "Paragraph inside Div")
                .AddChild(HtmlCssVocabulary.HtmlTags.Div, nestedDivNode => nestedDivNode
                    .AddText("Nested Div inside Paragraph")
                    .AddChild(HtmlCssVocabulary.HtmlTags.Span, "Nested Span inside nested Div")));

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
            .AddChild(HtmlCssVocabulary.HtmlTags.Div, divNode => divNode
                .WithPadding(15f, 11.25f, 7.5f, 3.75f)
                .AddText("Content"));

        var actual = Build(styles);

        var block = actual.Blocks.ShouldHaveSingleItem();
        block.Style.Padding.ShouldBe(new Spacing(15f, 11.25f, 7.5f, 3.75f));
        block.Geometry.ContentBoxRect.X.ShouldBe(block.Geometry.BorderBoxRect.X + 3.75f);
        block.Geometry.ContentBoxRect.Y.ShouldBe(block.Geometry.BorderBoxRect.Y + 15f);
    }

    private static StyleTreeBuilder BuildStyleTree() => new();

    private static PublishedLayoutTree Build(StyleTreeBuilder styles)
    {
        ILayoutGeometryStage stage = new LayoutGeometryConstruction(new ConstantTextMeasurer(10f, 9f, 3f));
        return stage.Build(new(styles, LayoutGeometryRequest.Default, null));
    }

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
