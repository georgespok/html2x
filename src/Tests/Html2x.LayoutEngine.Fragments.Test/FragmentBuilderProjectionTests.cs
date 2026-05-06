using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.LayoutEngine.Fragments.Test.Assertions;
using Html2x.LayoutEngine.Fragments.Test.Builders;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Fragments.Test;

public sealed class FragmentBuilderProjectionTests
{
    [Fact]
    public void Build_EmptyLayout_ReturnsNoRootFragments()
    {
        var fragments = new FragmentBuilder().Build(PublishedLayoutFragmentTestBuilder.Tree());

        fragments.ShouldNotBeNull();
        fragments.Blocks.ShouldBeEmpty();
    }

    [Fact]
    public void Build_BlockChildren_PreservesChildOrder()
    {
        var first = PublishedLayoutFragmentTestBuilder.Block(
            "body/div/p[0]",
            1,
            new RectPt(1f, 0f, 10f, 10f));
        var second = PublishedLayoutFragmentTestBuilder.Block(
            "body/div/p[1]",
            2,
            new RectPt(2f, 0f, 10f, 10f));
        var root = PublishedLayoutFragmentTestBuilder.Block(
            children: [first, second],
            flow:
            [
                new PublishedChildBlockItem(0, first),
                new PublishedChildBlockItem(1, second)
            ]);

        var fragments = Build(PublishedLayoutFragmentTestBuilder.Tree(root));

        var children = fragments.Blocks.ShouldHaveSingleItem().Children;
        children.Count.ShouldBe(2);
        children[0].ShouldBeOfType<BlockFragment>().Rect.X.ShouldBe(1f);
        children[1].ShouldBeOfType<BlockFragment>().Rect.X.ShouldBe(2f);
    }

    [Fact]
    public void Build_MixedFlow_PreservesInlineSegmentAndChildBlockOrder()
    {
        var childSegment = PublishedLayoutFragmentTestBuilder.Segment(
            PublishedLayoutFragmentTestBuilder.TextItem(0, "child"));
        var childInlineLayout = PublishedLayoutFragmentTestBuilder.InlineLayout(childSegment);
        var child = PublishedLayoutFragmentTestBuilder.Block(
            "body/div/p",
            1,
            inlineLayout: childInlineLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, childSegment)
            ]);
        var before = PublishedLayoutFragmentTestBuilder.Segment(
            PublishedLayoutFragmentTestBuilder.TextItem(0, "before"));
        var after = PublishedLayoutFragmentTestBuilder.Segment(
            PublishedLayoutFragmentTestBuilder.TextItem(0, "after"));
        var root = PublishedLayoutFragmentTestBuilder.Block(
            children: [child],
            inlineLayout: PublishedLayoutFragmentTestBuilder.InlineLayout(before, after),
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, before),
                new PublishedChildBlockItem(1, child),
                new PublishedInlineFlowSegmentItem(2, after)
            ]);

        var fragments = Build(PublishedLayoutFragmentTestBuilder.Tree(root));

        var rootFragment = fragments.Blocks.ShouldHaveSingleItem();
        rootFragment.Children[0].ShouldBeOfType<LineBoxFragment>()
            .Runs.ShouldHaveSingleItem().Text.ShouldBe("before");
        rootFragment.Children[1].ShouldBeOfType<BlockFragment>();
        rootFragment.Children[2].ShouldBeOfType<LineBoxFragment>()
            .Runs.ShouldHaveSingleItem().Text.ShouldBe("after");
    }

    [Fact]
    public void Build_FragmentIds_AreDeterministicAndUnique()
    {
        var nestedSegment = PublishedLayoutFragmentTestBuilder.Segment(
            PublishedLayoutFragmentTestBuilder.TextItem(0, "nested"));
        var nestedInlineLayout = PublishedLayoutFragmentTestBuilder.InlineLayout(nestedSegment);
        var nestedBlock = PublishedLayoutFragmentTestBuilder.Block(
            "body/div/p",
            1,
            inlineLayout: nestedInlineLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, nestedSegment)
            ]);
        var segment = PublishedLayoutFragmentTestBuilder.Segment(
            PublishedLayoutFragmentTestBuilder.TextItem(0, "root"));
        var inlineLayout = PublishedLayoutFragmentTestBuilder.InlineLayout(segment);
        var root = PublishedLayoutFragmentTestBuilder.Block(
            "body/div",
            0,
            inlineLayout: inlineLayout,
            children: [nestedBlock],
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, segment),
                new PublishedChildBlockItem(1, nestedBlock)
            ]);
        var layout = PublishedLayoutFragmentTestBuilder.Tree(root);

        var first = FragmentTreeAssertions.Flatten(Build(layout));
        var second = FragmentTreeAssertions.Flatten(Build(layout));

        var ids = first.Select(static fragment => fragment.FragmentId).ToList();
        ids.Count.ShouldBeGreaterThan(1);
        ids.All(static id => id > 0).ShouldBeTrue();
        ids.Distinct().Count().ShouldBe(ids.Count);
        ids.ShouldBe(second.Select(static fragment => fragment.FragmentId));
    }

    [Fact]
    public void Build_TextRuns_PreservesResolvedFontFromPublishedLayout()
    {
        var font = new FontKey("Inter", FontWeight.W700, FontStyle.Italic);
        var resolvedFont = new ResolvedFont("Inter", FontWeight.W700, FontStyle.Italic, "font://inter");
        var segment = PublishedLayoutFragmentTestBuilder.Segment(
            PublishedLayoutFragmentTestBuilder.TextItem(
                0,
                "alpha",
                font: font,
                resolvedFont: resolvedFont));
        var inlineLayout = PublishedLayoutFragmentTestBuilder.InlineLayout(segment);
        var block = PublishedLayoutFragmentTestBuilder.Block(
            inlineLayout: inlineLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, segment)
            ]);

        var fragments = new FragmentBuilder().Build(PublishedLayoutFragmentTestBuilder.Tree(block));

        var run = fragments.Blocks.ShouldHaveSingleItem()
            .Children
            .ShouldHaveSingleItem()
            .ShouldBeOfType<LineBoxFragment>()
            .Runs
            .ShouldHaveSingleItem();
        run.ResolvedFont.ShouldBe(resolvedFont);
    }

    [Fact]
    public void Build_TextItemWithNoRuns_EmitsNoLineFragment()
    {
        var segment = PublishedLayoutFragmentTestBuilder.Segment(
            PublishedLayoutFragmentTestBuilder.EmptyTextItem(0));
        var inlineLayout = PublishedLayoutFragmentTestBuilder.InlineLayout(segment);
        var block = PublishedLayoutFragmentTestBuilder.Block(
            inlineLayout: inlineLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, segment)
            ]);

        var fragments = new FragmentBuilder().Build(PublishedLayoutFragmentTestBuilder.Tree(block));

        fragments.Blocks.ShouldHaveSingleItem().Children.ShouldBeEmpty();
    }

    [Fact]
    public void Build_InlineLine_CopiesLineGeometryBaselineAndAlignment()
    {
        var lineRect = new RectPt(3f, 4f, 90f, 14f);
        var occupiedRect = new RectPt(8f, 4f, 44f, 14f);
        var textItem = PublishedLayoutFragmentTestBuilder.TextItem(
            0,
            "alpha",
            occupiedRect);
        var line = PublishedLayoutFragmentTestBuilder.Line(
            rect: lineRect,
            occupiedRect: occupiedRect,
            baselineY: 11f,
            lineHeight: 14f,
            textAlign: "center",
            items: textItem);
        var segment = PublishedLayoutFragmentTestBuilder.Segment(line);
        var inlineLayout = PublishedLayoutFragmentTestBuilder.InlineLayout(segment);
        var block = PublishedLayoutFragmentTestBuilder.Block(
            inlineLayout: inlineLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, segment)
            ]);

        var fragments = Build(PublishedLayoutFragmentTestBuilder.Tree(block));

        var lineFragment = fragments.Blocks.ShouldHaveSingleItem()
            .Children
            .ShouldHaveSingleItem()
            .ShouldBeOfType<LineBoxFragment>();
        lineFragment.Rect.ShouldBe(lineRect);
        lineFragment.OccupiedRect.ShouldBe(occupiedRect);
        lineFragment.BaselineY.ShouldBe(11f);
        lineFragment.LineHeight.ShouldBe(14f);
        lineFragment.TextAlign.ShouldBe("center");
    }

    [Fact]
    public void Build_InlineObject_ProjectsContentRecursively()
    {
        var innerSegment = PublishedLayoutFragmentTestBuilder.Segment(
            PublishedLayoutFragmentTestBuilder.TextItem(0, "inner"));
        var inlineObject = PublishedLayoutFragmentTestBuilder.Block(
            "body/span",
            1,
            formattingContext: FormattingContextKind.InlineBlock,
            inlineLayout: PublishedLayoutFragmentTestBuilder.InlineLayout(innerSegment),
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, innerSegment)
            ]);
        var segment = PublishedLayoutFragmentTestBuilder.Segment(
            PublishedLayoutFragmentTestBuilder.TextItem(0, "before"),
            PublishedLayoutFragmentTestBuilder.ObjectItem(1, inlineObject),
            PublishedLayoutFragmentTestBuilder.TextItem(2, "after"));
        var inlineLayout = PublishedLayoutFragmentTestBuilder.InlineLayout(segment);
        var root = PublishedLayoutFragmentTestBuilder.Block(
            inlineLayout: inlineLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, segment)
            ]);

        var fragments = Build(PublishedLayoutFragmentTestBuilder.Tree(root));

        FragmentTreeAssertions.EnumerateText(fragments.Blocks.ShouldHaveSingleItem())
            .ShouldBe(["before", "inner", "after"]);
    }

    [Fact]
    public void Build_ImageBlock_EmitsImageFragmentWithImageFacts()
    {
        var style = new ComputedStyle
        {
            BackgroundColor = new ColorRgba(10, 20, 30, 255)
        };
        var image = PublishedLayoutFragmentTestBuilder.Block(
            "body/img",
            0,
            new RectPt(1f, 2f, 30f, 20f),
            new RectPt(3f, 4f, 20f, 12f),
            style: style,
            image: new(
                "images/logo.png",
                new(30d, 20d),
                new(60d, 40d),
                ImageLoadStatus.Missing));

        var fragments = Build(PublishedLayoutFragmentTestBuilder.Tree(image));

        var imageFragment = fragments.Blocks.ShouldHaveSingleItem()
            .Children
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ImageFragment>();
        imageFragment.Src.ShouldBe("images/logo.png");
        imageFragment.AuthoredSizePx.ShouldBe(new(30d, 20d));
        imageFragment.IntrinsicSizePx.ShouldBe(new(60d, 40d));
        imageFragment.IsMissing.ShouldBeTrue();
        imageFragment.IsOversize.ShouldBeFalse();
        imageFragment.Rect.ShouldBe(new(1f, 2f, 30f, 20f));
        imageFragment.ContentRect.ShouldBe(new(3f, 4f, 20f, 12f));
        imageFragment.Style.BackgroundColor.ShouldBe(style.BackgroundColor);
    }

    [Fact]
    public void Build_RuleBlock_EmitsRuleFragment()
    {
        var rule = PublishedLayoutFragmentTestBuilder.Block(
            "body/hr",
            0,
            new RectPt(4f, 5f, 70f, 2f),
            rule: new());

        var fragments = Build(PublishedLayoutFragmentTestBuilder.Tree(rule));

        var ruleFragment = fragments.Blocks.ShouldHaveSingleItem()
            .Children
            .ShouldHaveSingleItem()
            .ShouldBeOfType<RuleFragment>();
        ruleFragment.Rect.ShouldBe(new(4f, 5f, 70f, 2f));
    }

    [Fact]
    public void Build_TableRoles_EmitSpecializedTableFragments()
    {
        var cell = PublishedLayoutFragmentTestBuilder.Block(
            "body/table/tr/td",
            2,
            role: FragmentDisplayRole.TableCell,
            table: new(null, null, 1, true));
        var row = PublishedLayoutFragmentTestBuilder.Block(
            "body/table/tr",
            1,
            role: FragmentDisplayRole.TableRow,
            table: new(null, 3, null, null),
            children: [cell],
            flow:
            [
                new PublishedChildBlockItem(0, cell)
            ]);
        var table = PublishedLayoutFragmentTestBuilder.Block(
            "body/table",
            role: FragmentDisplayRole.Table,
            table: new(2, null, null, null),
            children: [row],
            flow:
            [
                new PublishedChildBlockItem(0, row)
            ]);

        var fragments = Build(PublishedLayoutFragmentTestBuilder.Tree(table));

        var tableFragment = fragments.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableFragment>();
        tableFragment.DerivedColumnCount.ShouldBe(2);
        var rowFragment = tableFragment.Rows.ShouldHaveSingleItem();
        rowFragment.RowIndex.ShouldBe(3);
        var cellFragment = rowFragment.Cells.ShouldHaveSingleItem();
        cellFragment.ColumnIndex.ShouldBe(1);
        cellFragment.IsHeader.ShouldBeTrue();
    }

    private static FragmentTree Build(PublishedLayoutTree layout) => new FragmentBuilder().Build(layout);
}