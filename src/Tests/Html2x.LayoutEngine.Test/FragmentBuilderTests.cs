using AngleSharp;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.LayoutEngine.Test.Assertions;
using Html2x.LayoutEngine.Test.Builders;
using Moq;
using Shouldly;
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
        var fragments = CreateFragmentBuilder().Build(boxTree, CreateContext());

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
        var fragments = CreateFragmentBuilder().Build(boxTree, CreateContext());

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
        var fragments = CreateFragmentBuilder().Build(boxTree, CreateContext());

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
        var fragments = CreateFragmentBuilder().Build(boxTree, CreateContext());

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
        var ulBlock = new BlockBox(DisplayRole.Block) { Element = CreateElement("ul") };

        ulBlock.Children.Add(new BlockBox(DisplayRole.Block)
        {
            Element = CreateElement("li"),
            Children =
            {
                new InlineBox(DisplayRole.Inline) { TextContent = "• " },
                new InlineBox(DisplayRole.Inline) { TextContent = "item1" }
            }
        });

        ulBlock.Children.Add(new BlockBox(DisplayRole.Block)
        {
            Element = CreateElement("li"),
            Children =
            {
                new InlineBox(DisplayRole.Inline) { TextContent = "• " },
                new InlineBox(DisplayRole.Inline) { TextContent = "item2" }
            }
        });

        boxTree.Blocks.Add(ulBlock);

        var fragments = CreateFragmentBuilder().Build(boxTree, CreateContext());

        var ulFragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);

        var liFragment1 = ulFragment.Children[0].ShouldBeOfType<BlockFragment>();
        AssertLineContainsMarkerAndText(liFragment1, "• ", "item1");

        var liFragment2 = ulFragment.Children[1].ShouldBeOfType<BlockFragment>();
        AssertLineContainsMarkerAndText(liFragment2, "• ", "item2");
    }

    [Fact]
    public void Build_WithInlineBlockBetweenInlineRuns_PreservesTextOrderAcrossFragments()
    {
        var root = new BlockBox(DisplayRole.Block)
        {
            X = 0,
            Y = 0,
            Width = 300,
            Height = 120,
            Style = new ComputedStyle()
        };

        root.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            TextContent = "before",
            Parent = root,
            Style = new ComputedStyle()
        });

        var inlineBlock = new InlineBox(DisplayRole.InlineBlock)
        {
            Parent = root,
            Style = new ComputedStyle()
        };

        var inlineBlockContent = new BlockBox(DisplayRole.Block)
        {
            Parent = inlineBlock,
            IsAnonymous = true,
            IsInlineBlockContext = true,
            Style = new ComputedStyle(),
            Width = 120,
            Height = 20
        };

        inlineBlockContent.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            TextContent = "inner",
            Parent = inlineBlockContent,
            Style = new ComputedStyle()
        });

        inlineBlock.Children.Add(inlineBlockContent);
        root.Children.Add(inlineBlock);

        root.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            TextContent = "after",
            Parent = root,
            Style = new ComputedStyle()
        });

        var tree = new BoxTree();
        tree.Blocks.Add(root);

        var fragments = CreateFragmentBuilder().Build(tree, CreateContext());
        var parent = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);

        var orderedTexts = EnumerateTextRuns(parent).ToList();
        orderedTexts.ShouldBe(["before", "inner", "after"]);
    }

    [Fact]
    public void LayoutSnapshotPayload_RepeatedRuns_PreservesTraversalOrderAndSequenceIds()
    {
        var runs = new List<IReadOnlyList<string>>();
        var sequenceRuns = new List<IReadOnlyList<int>>();

        for (var iteration = 0; iteration < 3; iteration++)
        {
            var fragmentTree = CreateFragmentBuilder().Build(BuildAmbiguousTopLevelOrderTree(), CreateContext());
            var layout = new HtmlLayout();
            layout.Pages.Add(new LayoutPage(PaperSizes.A4, new Spacing(), fragmentTree.Blocks));

            var snapshot = LayoutSnapshotMapper.From(layout);
            var topLevelTexts = snapshot.Pages[0].Fragments
                .Select(GetFirstText)
                .ToList();
            runs.Add(topLevelTexts);

            var sequenceIds = Flatten(snapshot.Pages[0].Fragments)
                .Select(static fragment => fragment.SequenceId)
                .ToList();
            sequenceRuns.Add(sequenceIds);
        }

        runs[1].ShouldBe(runs[0]);
        runs[2].ShouldBe(runs[0]);
        runs[0].ShouldBe(["beta", "alpha"]);

        sequenceRuns[1].ShouldBe(sequenceRuns[0]);
        sequenceRuns[2].ShouldBe(sequenceRuns[0]);
        sequenceRuns[0].ShouldBe(Enumerable.Range(1, sequenceRuns[0].Count).ToList());
    }

    // Helpers
    private static FragmentBuilder CreateFragmentBuilder() => new FragmentBuilder();

    private static BlockBoxBuilder BuildBoxTree() => new BlockBoxBuilder();

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

        return new FragmentBuildContext(
            new NoopImageProvider(),
            Directory.GetCurrentDirectory(),
            (long)(10 * 1024 * 1024),
            textMeasurer.Object,
            fontSource.Object);
    }

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
            run.Origin.Y.ShouldBe(line.BaselineY, 0.01);
        }
    }

    private static IEnumerable<string> EnumerateTextRuns(CoreFragment fragment)
    {
        if (fragment is LineBoxFragment line)
        {
            foreach (var run in line.Runs)
            {
                var text = run.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return text;
                }
            }
        }

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var text in EnumerateTextRuns(child))
            {
                yield return text;
            }
        }
    }

    private static BoxTree BuildAmbiguousTopLevelOrderTree()
    {
        var beta = new BlockBox(DisplayRole.Block)
        {
            X = 0,
            Y = 0,
            Width = 100,
            Height = 20,
            Style = new ComputedStyle()
        };
        beta.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            TextContent = "beta",
            Parent = beta,
            Style = new ComputedStyle()
        });

        var alpha = new BlockBox(DisplayRole.Block)
        {
            X = 0,
            Y = 0,
            Width = 100,
            Height = 20,
            Style = new ComputedStyle()
        };
        alpha.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            TextContent = "alpha",
            Parent = alpha,
            Style = new ComputedStyle()
        });

        var tree = new BoxTree();
        tree.Blocks.Add(beta);
        tree.Blocks.Add(alpha);
        return tree;
    }

    private static string GetFirstText(Abstractions.Diagnostics.FragmentSnapshot fragment)
    {
        if (!string.IsNullOrWhiteSpace(fragment.Text))
        {
            return fragment.Text.Trim();
        }

        foreach (var child in fragment.Children)
        {
            var text = GetFirstText(child);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<Abstractions.Diagnostics.FragmentSnapshot> Flatten(IReadOnlyList<Abstractions.Diagnostics.FragmentSnapshot> fragments)
    {
        foreach (var fragment in fragments)
        {
            yield return fragment;

            foreach (var child in Flatten(fragment.Children))
            {
                yield return child;
            }
        }
    }
}
