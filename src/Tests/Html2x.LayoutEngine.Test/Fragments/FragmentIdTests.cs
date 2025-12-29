using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Test.Builders;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;
using Shouldly;
using Xunit;
using CoreFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.Fragments;

public class FragmentIdTests
{
    [Fact]
    public void Build_AssignsMonotonicFragmentIds()
    {
        // Arrange: block with inline text and a nested block containing an image-like child (specialized stage)
        var boxTree = new BlockBoxBuilder()
            .Block(0, 0, 200, 120)
                .Inline("Hello")
                .Block(20, 40, 60, 30) // nested block to create another fragment
            .Up()
            .BuildTree();

        var builder = new FragmentBuilder();
        var context = CreateContext();

        // Act
        var fragments = builder.Build(boxTree, context);

        // Collect all fragments (top-level + children)
        var all = new List<CoreFragment>();
        foreach (var block in fragments.Blocks)
        {
            all.Add(block);
            all.AddRange(Flatten(block));
        }

        // Assert
        all.Count.ShouldBeGreaterThan(1);
        var ids = all.Select(f => f.FragmentId).ToList();
        ids.ShouldBe(ids.OrderBy(x => x));          // monotonic increasing
        ids.Distinct().Count().ShouldBe(ids.Count); // unique
        ids.First().ShouldBe(1);                    // starts at 1
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

    private static FragmentBuildContext CreateContext()
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns(0f);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((0f, 0f));

        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(x => x.Resolve(It.IsAny<FontKey>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));

        return new FragmentBuildContext(new NoopImageProvider(), ".", 1024 * 1024, textMeasurer.Object, fontSource.Object);
    }
}
