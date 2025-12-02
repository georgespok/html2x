using AngleSharp;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.LayoutEngine.Test.Assertions;
using Html2x.LayoutEngine.Test.Builders;
using Shouldly;
using System.IO;
using CoreFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test;

public class FragmentBuilderTests
{
    [Fact]
    public void Build_WithSingleBlock_CreatesBlockFragment()
    {
        // Arrange
        var boxTree = BuildBoxTree()
            .Block(10, 20, 100, 50)
            .Up()
            .BuildTree();

        // Act
        var fragments = CreateFragmentBuilder().Build(boxTree, new FragmentBuildContext(new NoopImageProvider(), Directory.GetCurrentDirectory(), (long)(10 * 1024 * 1024)));

        // Assert
        var fragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);
        fragment.Rect.X.ShouldBe(10f);
        fragment.Rect.Y.ShouldBe(20f);
        fragment.Rect.Width.ShouldBe(100f);
        fragment.Rect.Height.ShouldBe(50f);
    }

    [Fact]
    public void Build_WithBlockBorder_CreatesBlockFragment()
    {
        // Arrange
        var boxTree = BuildBoxTree()
            .Block(10, 20, 100, 50, style: new ComputedStyle
            {
                Borders = BorderEdges.Uniform(
                    new(0.75f, new ColorRgba(0, 0, 0, 255), BorderLineStyle.Solid ))
            })
            .Up()
            .BuildTree();

        // Act
        var fragments = CreateFragmentBuilder().Build(boxTree, new FragmentBuildContext(new NoopImageProvider(), Directory.GetCurrentDirectory(), (long)(10 * 1024 * 1024)));

        // Assert
        var fragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);
        fragment.Style.Borders.ShouldBeEquivalentTo(
            BorderEdges.Uniform(new BorderSide(0.75f, ColorRgba.Black, BorderLineStyle.Solid))
        );

    }

    [Fact]
    public void Build_WithDivContainingInlineSpanAndBlockParagraph_ConvertsAllToFragments()
    {
        // Arrange: Div with inline span (text) and block paragraph (text)
        var boxTree = BuildBoxTree()
            .Block(0, 0, 595, 200)
                .Inline("Span inside Div")
                .Block(0, 50, 595, 40)
                    .Inline("Paragraph inside Div")
                    .Up()
                .Up()
            .Up()
            .BuildTree();

        // Act
        var fragments = CreateFragmentBuilder().Build(boxTree, new FragmentBuildContext(new NoopImageProvider(), Directory.GetCurrentDirectory(), (long)(10 * 1024 * 1024)));

        // Assert: One top-level BlockFragment for div
        var divFragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);

        // Div fragment should have BlockFragment for paragraph AND LineBoxFragment for span text
        AssertFragment(divFragment).HasChildCount(2);

        var pFragment = divFragment.Children[0].ShouldBeOfType<BlockFragment>();
        var pLine = AssertFragment(pFragment).HasChildCount(1).GetChild<LineBoxFragment>(0);
        pLine.Runs.Count.ShouldBe(1);
        pLine.Runs[0].Text.ShouldBe("Paragraph inside Div");

        var spanLine = divFragment.Children[1].ShouldBeOfType<LineBoxFragment>();
        spanLine.Runs.Count.ShouldBe(1);
        spanLine.Runs[0].Text.ShouldBe("Span inside Div");
    }

    [Fact]
    public void Build_WithDeeplyNestedBlocks_ConvertsAllInlineTextToFragments()
    {
        // Arrange: Div → span + P → text + nested Div → nested span
        var boxTree = BuildBoxTree()
            .Block(0, 0, 595, 300)
                .Inline("Span inside Div")
                .Block(0, 50, 595, 200)
                    .Inline("Paragraph inside Div")
                    .Block(0, 100, 595, 80)
                        .Inline("Nested Span inside nested Div")
                        .Up()
                    .Up()
                .Up()
            .Up()
            .BuildTree();

        // Act
        var fragments = CreateFragmentBuilder().Build(boxTree, new FragmentBuildContext(new NoopImageProvider(), Directory.GetCurrentDirectory(), (long)(10 * 1024 * 1024)));

        // Assert: One top-level BlockFragment
        var divFragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);

        // Outer div has p BlockFragment + span LineBoxFragment
        AssertFragment(divFragment).HasChildCount(2);

        // P fragment has nested div BlockFragment + text LineBoxFragment
        var pFragment = divFragment.Children[0].ShouldBeOfType<BlockFragment>();
        AssertFragment(pFragment).HasChildCount(2);

        var nestedDivFragment = pFragment.Children[0].ShouldBeOfType<BlockFragment>();
        var nestedSpanLine = AssertFragment(nestedDivFragment).HasChildCount(1).GetChild<LineBoxFragment>(0);
        nestedSpanLine.Runs[0].Text.ShouldBe("Nested Span inside nested Div");

        var pTextLine = pFragment.Children[1].ShouldBeOfType<LineBoxFragment>();
        pTextLine.Runs[0].Text.ShouldBe("Paragraph inside Div");

        // Outer span
        var outerSpanLine = divFragment.Children[1].ShouldBeOfType<LineBoxFragment>();
        outerSpanLine.Runs[0].Text.ShouldBe("Span inside Div");
    }

    [Fact]
    public void Build_WithUnorderedList_AddsBulletMarkers()
    {
        var boxTree = new BoxTree();
        var ulBlock = new BlockBox { Element = CreateElement("ul") };

        ulBlock.Children.Add(new BlockBox
        {
            Element = CreateElement("li"),
            Children =
            {
                new InlineBox { TextContent = "• " },
                new InlineBox { TextContent = "item1" }
            }
        });

        ulBlock.Children.Add(new BlockBox
        {
            Element = CreateElement("li"),
            Children =
            {
                new InlineBox { TextContent = "• " },
                new InlineBox { TextContent = "item2" }
            }
        });

        boxTree.Blocks.Add(ulBlock);

        var fragments = CreateFragmentBuilder().Build(boxTree, new FragmentBuildContext(new NoopImageProvider(), Directory.GetCurrentDirectory(), (long)(10 * 1024 * 1024)));

        var ulFragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);

        var liFragment1 = ulFragment.Children[0].ShouldBeOfType<BlockFragment>();
        AssertLineContainsMarkerAndText(liFragment1, "• ", "item1");

        var liFragment2 = ulFragment.Children[1].ShouldBeOfType<BlockFragment>();
        AssertLineContainsMarkerAndText(liFragment2, "• ", "item2");
    }

    // Helpers
    private static FragmentBuilder CreateFragmentBuilder() => new FragmentBuilder();

    private static BlockBoxBuilder BuildBoxTree() => new BlockBoxBuilder();

    private static FragmentTreeAssertion AssertFragmentTree(FragmentTree tree)
    {
        return new FragmentTreeAssertion(tree);
    }

    private static FragmentAssertion AssertFragment(CoreFragment fragment)
    {
        return new FragmentAssertion(fragment);
    }

    private static AngleSharp.Dom.IElement CreateElement(string tag)
    {
        return BrowsingContext.New(Configuration.Default)
            .OpenNewAsync().Result.CreateElement(tag);
    }

    private static void AssertLineContainsMarkerAndText(BlockFragment fragment, string marker, string text)
    {
        fragment.Children.Count.ShouldBe(1, "Line box fragments should collapse marker and text into a single entry.");

        var line = fragment.Children[0].ShouldBeOfType<LineBoxFragment>();
        line.Runs.Count.ShouldBe(2);
        line.Runs[0].Text.ShouldBe(marker);
        line.Runs[1].Text.ShouldBe(text);

        foreach (var run in line.Runs)
        {
            (run.Origin.Y + run.Ascent).ShouldBe(line.BaselineY, 0.01);
        }
    }
}
