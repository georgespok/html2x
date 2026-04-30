using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Test.Builders;
using Moq;
using Shouldly;
using CoreFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.Fragments;

public class FragmentIdTests
{
    [Fact]
    public void Build_AssignsUniqueFragmentIds()
    {
        var nestedSegment = PublishedLayoutTestBuilder.Segment(PublishedLayoutTestBuilder.TextItem(0, "Nested"));
        var nestedInlineLayout = PublishedLayoutTestBuilder.InlineLayout(nestedSegment);
        var nestedBlock = PublishedLayoutTestBuilder.Block(
            nodePath: "body/div/p",
            sourceOrder: 1,
            inlineLayout: nestedInlineLayout,
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, nestedSegment)
            ]);
        var segment = PublishedLayoutTestBuilder.Segment(PublishedLayoutTestBuilder.TextItem(0, "Hello"));
        var inlineLayout = PublishedLayoutTestBuilder.InlineLayout(segment);
        var root = PublishedLayoutTestBuilder.Block(
            nodePath: "body/div",
            sourceOrder: 0,
            inlineLayout: inlineLayout,
            children: [nestedBlock],
            flow:
            [
                new PublishedInlineFlowSegmentItem(0, segment),
                new PublishedChildBlockItem(1, nestedBlock)
            ]);
        var builder = new FragmentBuilder();
        var fontSource = CreateFontSource();

        var fragments = builder.Build(PublishedLayoutTestBuilder.Tree(root), fontSource);

        var all = new List<CoreFragment>();
        foreach (var block in fragments.Blocks)
        {
            all.Add(block);
            all.AddRange(Flatten(block));
        }

        all.Count.ShouldBeGreaterThan(1);
        var ids = all.Select(f => f.FragmentId).ToList();
        ids.All(static id => id > 0).ShouldBeTrue();
        ids.Distinct().Count().ShouldBe(ids.Count);
        ids.Min().ShouldBe(1);
    }

    private static IEnumerable<CoreFragment> Flatten(BlockFragment block)
    {
        foreach (var child in block.Children)
        {
            yield return child;
            if (child is BlockFragment nested)
            {
                foreach (var grand in Flatten(nested))
                {
                    yield return grand;
                }
            }
        }
    }

    private static IFontSource CreateFontSource()
    {
        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(x => x.Resolve(It.IsAny<FontKey>(), It.IsAny<string>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));

        return fontSource.Object;
    }
}
