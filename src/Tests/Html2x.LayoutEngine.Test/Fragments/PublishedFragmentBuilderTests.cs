using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Test.Builders;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;
using Shouldly;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.Fragments;

public sealed class PublishedFragmentBuilderTests
{
    [Fact]
    public void Build_WithPublishedBlock_UsesPublishedGeometryAndStyle()
    {
        var style = new ComputedStyle
        {
            Borders = BorderEdges.Uniform(new(0.75f, ColorRgba.Black, BorderLineStyle.Solid))
        };
        var block = PublishedLayoutTestBuilder.Block(
            rect: new RectPt(10f, 20f, 100f, 50f),
            style: style);

        var fragments = Build(PublishedLayoutTestBuilder.Tree(block));

        var fragment = fragments.Blocks.ShouldHaveSingleItem();
        fragment.Rect.ShouldBe(new(10f, 20f, 100f, 50f));
        fragment.Style.Borders.ShouldBe(style.Borders);
    }

    [Fact]
    public void Build_WithPublishedDisplayFacts_UsesPublishedDisplayMetadata()
    {
        var block = PublishedLayoutTestBuilder.Block(
            role: FragmentDisplayRole.ListItem,
            markerOffset: 12f);

        var fragments = Build(PublishedLayoutTestBuilder.Tree(block));

        var fragment = fragments.Blocks.ShouldHaveSingleItem();
        fragment.DisplayRole.ShouldBe(FragmentDisplayRole.ListItem);
        fragment.FormattingContext.ShouldBe(FormattingContextKind.Block);
        fragment.MarkerOffset.ShouldBe(12f);
    }

    [Fact]
    public void Build_WithPublishedInlineObject_PreservesInlineItemOrder()
    {
        var innerLayout = PublishedLayoutTestBuilder.InlineLayout(
            PublishedLayoutTestBuilder.Segment(PublishedLayoutTestBuilder.TextItem(0, "inner")));
        var inlineObject = PublishedLayoutTestBuilder.Block(
            "body/span",
            1,
            formattingContext: FormattingContextKind.InlineBlock,
            inlineLayout: innerLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, innerLayout.Segments[0])
            ]);
        var segment = PublishedLayoutTestBuilder.Segment(
            PublishedLayoutTestBuilder.TextItem(0, "before"),
            PublishedLayoutTestBuilder.ObjectItem(1, inlineObject),
            PublishedLayoutTestBuilder.TextItem(2, "after"));
        var inlineLayout = PublishedLayoutTestBuilder.InlineLayout(segment);
        var root = PublishedLayoutTestBuilder.Block(
            inlineLayout: inlineLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, segment)
            ]);

        var fragments = Build(PublishedLayoutTestBuilder.Tree(root));

        EnumerateText(fragments.Blocks.ShouldHaveSingleItem()).ShouldBe(["before", "inner", "after"]);
    }

    [Fact]
    public void Build_PublishedInlineSegment_CopiesLineFactsAndFonts()
    {
        var segment = PublishedLayoutTestBuilder.Segment(PublishedLayoutTestBuilder.TextItem(0, "alpha"));
        var inlineLayout = PublishedLayoutTestBuilder.InlineLayout(segment);
        var block = PublishedLayoutTestBuilder.Block(
            inlineLayout: inlineLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, segment)
            ]);

        var fragments = Build(PublishedLayoutTestBuilder.Tree(block));

        var line = fragments.Blocks.ShouldHaveSingleItem()
            .Children
            .ShouldHaveSingleItem()
            .ShouldBeOfType<LineBoxFragment>();
        line.Runs.Select(static run => run.Text).ShouldBe(["alpha"]);
        line.Rect.ShouldBe(segment.Lines[0].Rect);
        line.OccupiedRect.ShouldBe(segment.Lines[0].Items[0].Rect);
        line.BaselineY.ShouldBe(segment.Lines[0].BaselineY);
        line.LineHeight.ShouldBe(segment.Lines[0].LineHeight);
        line.Runs.All(static run => run.ResolvedFont is not null).ShouldBeTrue();
    }

    [Fact]
    public void Build_WithPublishedImageAndRuleFacts_UsesPublishedFacts()
    {
        var image = PublishedLayoutTestBuilder.Block(
            "body/img",
            0,
            new RectPt(1f, 2f, 30f, 20f),
            image: new(
                "images/logo.png",
                new(30d, 20d),
                new(60d, 40d),
                ImageLoadStatus.Oversize));
        var rule = PublishedLayoutTestBuilder.Block(
            "body/hr",
            1,
            new RectPt(4f, 5f, 70f, 2f),
            rule: new());

        var fragments = Build(PublishedLayoutTestBuilder.Tree(image, rule));

        var imageFragment = fragments.Blocks[0].Children.ShouldHaveSingleItem().ShouldBeOfType<ImageFragment>();
        imageFragment.Src.ShouldBe("images/logo.png");
        imageFragment.IsOversize.ShouldBeTrue();
        imageFragment.Rect.ShouldBe(new(1f, 2f, 30f, 20f));
        fragments.Blocks[1].Children.ShouldHaveSingleItem().ShouldBeOfType<RuleFragment>();
    }

    [Fact]
    public void Build_WithPublishedTableFacts_PreservesTableChildMetadata()
    {
        var cell = PublishedLayoutTestBuilder.Block(
            "body/table/tr/td",
            2,
            role: FragmentDisplayRole.TableCell,
            table: new(null, null, 1, true));
        var row = PublishedLayoutTestBuilder.Block(
            "body/table/tr",
            1,
            role: FragmentDisplayRole.TableRow,
            table: new(null, 3, null, null),
            children: [cell],
            flow:
            [
                new PublishedChildBlockItem(0, cell)
            ]);
        var table = PublishedLayoutTestBuilder.Block(
            "body/table",
            role: FragmentDisplayRole.Table,
            table: new(2, null, null, null),
            children: [row],
            flow:
            [
                new PublishedChildBlockItem(0, row)
            ]);

        var fragments = Build(PublishedLayoutTestBuilder.Tree(table));

        var tableFragment = fragments.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableFragment>();
        tableFragment.DerivedColumnCount.ShouldBe(2);
        var rowFragment = tableFragment.Rows.ShouldHaveSingleItem();
        rowFragment.RowIndex.ShouldBe(3);
        var cellFragment = rowFragment.Cells.ShouldHaveSingleItem();
        cellFragment.ColumnIndex.ShouldBe(1);
        cellFragment.IsHeader.ShouldBeTrue();
    }

    [Fact]
    public void Build_WithPublishedFlow_PreservesInlineAndBlockChildOrder()
    {
        var childSegment = PublishedLayoutTestBuilder.Segment(PublishedLayoutTestBuilder.TextItem(0, "child"));
        var childInlineLayout = PublishedLayoutTestBuilder.InlineLayout(childSegment);
        var child = PublishedLayoutTestBuilder.Block(
            "body/div/p",
            1,
            inlineLayout: childInlineLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, childSegment)
            ]);
        var before = PublishedLayoutTestBuilder.Segment(PublishedLayoutTestBuilder.TextItem(0, "before"));
        var after = PublishedLayoutTestBuilder.Segment(PublishedLayoutTestBuilder.TextItem(0, "after"));
        var root = PublishedLayoutTestBuilder.Block(
            children: [child],
            inlineLayout: PublishedLayoutTestBuilder.InlineLayout(before, after),
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, before),
                new PublishedChildBlockItem(1, child),
                new PublishedInlineFlowSegmentItem(2, after)
            ]);

        var fragments = Build(PublishedLayoutTestBuilder.Tree(root));

        var rootFragment = fragments.Blocks.ShouldHaveSingleItem();
        rootFragment.Children[0].ShouldBeOfType<LineBoxFragment>()
            .Runs.ShouldHaveSingleItem().Text.ShouldBe("before");
        rootFragment.Children[1].ShouldBeOfType<BlockFragment>();
        rootFragment.Children[2].ShouldBeOfType<LineBoxFragment>()
            .Runs.ShouldHaveSingleItem().Text.ShouldBe("after");
    }

    private static FragmentTree Build(PublishedLayoutTree layout) => new FragmentBuilder().Build(layout);

    private static IEnumerable<string> EnumerateText(LayoutFragment fragment)
    {
        if (fragment is LineBoxFragment line)
        {
            foreach (var run in line.Runs)
            {
                yield return run.Text;
            }
        }

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var text in EnumerateText(child))
            {
                yield return text;
            }
        }
    }
}