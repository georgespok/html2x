using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using AngleSharp;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Fragment.Stages;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.Builders;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;
using Shouldly;
using Html2x.LayoutEngine.Geometry;

namespace Html2x.LayoutEngine.Test.Text;

public class InlineFragmentStageTests
{
    [Fact]
    public void InlineSiblingsWithSameBaseline_AreMergedIntoSingleLine()
    {
        // Arrange: build a single block with sibling inline nodes via the builder helpers
        var boxTree = new BlockBoxBuilder()
            .Block(10, 20, 200, 40, style: new ComputedStyle { FontSizePt = 12 })
                .Inline("This is ", new ComputedStyle { FontSizePt = 12 })
                .Inline("bold", new ComputedStyle { FontSizePt = 12, Bold = true })
                .Inline(" and ", new ComputedStyle { FontSizePt = 12 })
                .Inline("italic", new ComputedStyle { FontSizePt = 12, Italic = true })
            .Inline(" text.", new ComputedStyle { FontSizePt = 12 })
            .Up()
            .BuildTree();

        var context = CreateContext(new FakeTextMeasurer(0f, 9f, 3f));
        PrepareInlineLayouts(boxTree, context);
        var state = new FragmentBuildState(boxTree, context);
        
        // Act
        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        
        // Assert
        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        var line = fragment.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        line.Runs.Count.ShouldBe(5);
        string.Concat(line.Runs.Select(r => r.Text)).ShouldBe("This is bold and italic text.");

        foreach (var run in line.Runs)
        {
            run.Origin.Y.ShouldBe(line.BaselineY, 0.01);
        }

        for (var i = 1; i < line.Runs.Count; i++)
        {
            line.Runs[i].Origin.X.ShouldBeGreaterThanOrEqualTo(line.Runs[i - 1].Origin.X);
        }
    }

    [Fact]
    public void InlineText_UsesMeasuredWidthAndMetrics()
    {
        var boxTree = new BlockBoxBuilder()
            .Block(0, 0, 200, 40, style: new ComputedStyle { FontSizePt = 12 })
                .Inline("Measure me", new ComputedStyle { FontSizePt = 12 })
            .Up()
            .BuildTree();

        var context = CreateContext(new FakeTextMeasurer(4f, 9f, 3f));
        PrepareInlineLayouts(boxTree, context);
        var state = new FragmentBuildState(boxTree, context);

        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        var line = fragment.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        line.Rect.Width.ShouldBe(200f);
        line.OccupiedRect.Width.ShouldBe(40f);
        line.LineHeight.ShouldBe(14.4f, 0.001f);
        line.BaselineY.ShouldBe(line.Rect.Top + 9f, 0.001f);
        line.Runs.ShouldHaveSingleItem().ResolvedFont?.SourceId.ShouldBe("test");
    }

    [Fact]
    public void InlineRuns_DifferentAscent_UseMaxAscentForBaseline()
    {
        var boxTree = new BlockBoxBuilder()
            .Block(0, 0, 500, 40, style: new ComputedStyle { FontSizePt = 12 })
                .Inline("small", new ComputedStyle { FontSizePt = 12 })
                .Inline("BIG", new ComputedStyle { FontSizePt = 20 })
            .Up()
            .BuildTree();

        var context = CreateContext(new SizeBasedTextMeasurer(5f));
        PrepareInlineLayouts(boxTree, context);
        var state = new FragmentBuildState(boxTree, context);

        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        var line = fragment.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        var expectedAscent = 20f * 0.7f;
        line.BaselineY.ShouldBe(line.Rect.Top + expectedAscent, 0.01f);
    }

    [Fact]
    public void InlineRuns_PropagateDecorations()
    {
        var boxTree = new BlockBoxBuilder()
            .Block(0, 0, 500, 40, style: new ComputedStyle { FontSizePt = 12 })
                .Inline("plain ", new ComputedStyle { FontSizePt = 12 })
                .Inline("underline", new ComputedStyle { FontSizePt = 12, Decorations = TextDecorations.Underline })
                .Inline(" strike", new ComputedStyle { FontSizePt = 12, Decorations = TextDecorations.LineThrough })
            .Up()
            .BuildTree();

        var context = CreateContext(new FakeTextMeasurer(4f, 9f, 3f));
        PrepareInlineLayouts(boxTree, context);
        var state = new FragmentBuildState(boxTree, context);

        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        var line = fragment.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        line.Runs.Count.ShouldBe(3);
        line.Runs[0].Decorations.ShouldBe(TextDecorations.None);
        line.Runs[1].Decorations.ShouldBe(TextDecorations.Underline);
        line.Runs[2].Decorations.ShouldBe(TextDecorations.LineThrough);
    }

    [Fact]
    public void InlineObjectBetweenTextRuns_IsEmittedInOrderWithoutDeferredAppend()
    {
        var root = new BlockBox(DisplayRole.Block)
        {
            X = 0,
            Y = 0,
            Width = 400,
            Height = 120,
            Style = new ComputedStyle { FontSizePt = 12 }
        };

        root.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = root,
            TextContent = "before",
            Style = root.Style
        });

        var inlineBlock = new InlineBox(DisplayRole.InlineBlock)
        {
            Parent = root,
            Style = new ComputedStyle { Padding = new Spacing(2, 2, 2, 2) }
        };

        var inlineBlockContent = new BlockBox(DisplayRole.Block)
        {
            Parent = inlineBlock,
            IsAnonymous = true,
            IsInlineBlockContext = true,
            Width = 80,
            Height = 20,
            Style = new ComputedStyle { Padding = new Spacing(2, 2, 2, 2), FontSizePt = 12 }
        };
        inlineBlockContent.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = inlineBlockContent,
            TextContent = "inner",
            Style = inlineBlockContent.Style
        });

        inlineBlock.Children.Add(inlineBlockContent);
        root.Children.Add(inlineBlock);

        root.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = root,
            TextContent = "after",
            Style = root.Style
        });

        var boxTree = new BoxTree();
        boxTree.Blocks.Add(root);

        var context = CreateContext(new FakeTextMeasurer(4f, 9f, 3f));
        PrepareInlineLayouts(boxTree, context);
        var state = new FragmentBuildState(boxTree, context);

        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        fragment.Children.Count.ShouldBe(3);

        var before = fragment.Children[0].ShouldBeOfType<LineBoxFragment>();
        var emittedInlineObject = fragment.Children[1].ShouldBeOfType<BlockFragment>();
        var after = fragment.Children[2].ShouldBeOfType<LineBoxFragment>();

        before.Runs.Select(static run => run.Text).ShouldBe(["before"]);
        emittedInlineObject.Children
            .OfType<LineBoxFragment>()
            .SelectMany(static line => line.Runs)
            .Select(static run => run.Text)
            .ShouldContain("inner");
        after.Runs.Select(static run => run.Text).ShouldBe(["after"]);
    }

    [Fact]
    public void InlineFlowSeparatedByBlockChild_EmitsSegmentsAroundBlockFragment()
    {
        var root = new BlockBox(DisplayRole.Block)
        {
            X = 0,
            Y = 0,
            Width = 400,
            Height = 120,
            Style = new ComputedStyle { FontSizePt = 12 }
        };

        root.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = root,
            TextContent = "before",
            Style = root.Style
        });

        var childBlock = new BlockBox(DisplayRole.Block)
        {
            Parent = root,
            X = 0,
            Y = 30,
            Width = 400,
            Height = 30,
            Style = new ComputedStyle { FontSizePt = 12 }
        };
        childBlock.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = childBlock,
            TextContent = "inside",
            Style = childBlock.Style
        });
        root.Children.Add(childBlock);

        root.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = root,
            TextContent = "after",
            Style = root.Style
        });

        var boxTree = new BoxTree();
        boxTree.Blocks.Add(root);

        var context = CreateContext(new FakeTextMeasurer(4f, 9f, 3f));
        PrepareInlineLayouts(boxTree, context);
        var state = new FragmentBuildState(boxTree, context);

        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        fragment.Children.Count.ShouldBe(3);

        var before = fragment.Children[0].ShouldBeOfType<LineBoxFragment>();
        var childFragment = fragment.Children[1].ShouldBeOfType<BlockFragment>();
        var after = fragment.Children[2].ShouldBeOfType<LineBoxFragment>();

        before.Runs.Select(static run => run.Text).ShouldBe(["before"]);
        childFragment.Children
            .OfType<LineBoxFragment>()
            .SelectMany(static line => line.Runs)
            .Select(static run => run.Text)
            .ShouldBe(["inside"]);
        after.Runs.Select(static run => run.Text).ShouldBe(["after"]);
    }

    [Fact]
    public void ListItemFallback_ForOrderedList_UsesSiblingOrdinalMarker()
    {
        var root = new BlockBox(DisplayRole.Block)
        {
            X = 0,
            Y = 0,
            Width = 500,
            Height = 200,
            Style = new ComputedStyle { FontSizePt = 12 },
            Element = CreateElement("div")
        };

        var orderedList = new BlockBox(DisplayRole.Block)
        {
            X = 0,
            Y = 0,
            Width = 500,
            Height = 200,
            Style = new ComputedStyle { FontSizePt = 12 },
            Element = CreateElement("ol"),
            Parent = root
        };

        root.Children.Add(orderedList);
        orderedList.Children.Add(CreateListItem("First", orderedList));
        orderedList.Children.Add(CreateListItem("Second", orderedList));

        var boxTree = new BoxTree();
        boxTree.Blocks.Add(root);

        var context = CreateContext(new FakeTextMeasurer(4f, 9f, 3f));
        PrepareInlineLayouts(boxTree, context);
        var state = new FragmentBuildState(boxTree, context);

        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        var rootFragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        var listItemFragments = EnumerateBlockFragments(rootFragment)
            .Where(fragment => fragment.DisplayRole == FragmentDisplayRole.ListItem)
            .ToList();

        listItemFragments.Count.ShouldBe(2);

        var firstLine = listItemFragments[0].Children.OfType<LineBoxFragment>().First();
        var secondLine = listItemFragments[1].Children.OfType<LineBoxFragment>().First();

        firstLine.Runs[0].Text.ShouldBe("1. ");
        secondLine.Runs[0].Text.ShouldBe("2. ");
    }

    private static FragmentBuildContext CreateContext(ITextMeasurer textMeasurer)
    {
        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(x => x.Resolve(It.IsAny<FontKey>(), It.IsAny<string>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));

        return new FragmentBuildContext(
            new NoopImageProvider(),
            Directory.GetCurrentDirectory(),
            (long)(10 * 1024 * 1024),
            textMeasurer,
            fontSource.Object);
    }

    private static void PrepareInlineLayouts(BoxTree boxTree, FragmentBuildContext context)
    {
        var inlineEngine = new InlineLayoutEngine(new FontMetricsProvider(), context.TextMeasurer, new DefaultLineHeightStrategy());

        foreach (var block in boxTree.Blocks)
        {
            EnsureUsedGeometry(block);
            PrepareInlineLayout(block, inlineEngine);
        }
    }

    private static void EnsureUsedGeometry(BlockBox block)
    {
        if (block.UsedGeometry is null)
        {
            var padding = block.Style.Padding.Safe();
            var border = Spacing.FromBorderEdges(block.Style.Borders).Safe();
            block.UsedGeometry = BoxGeometryFactory.FromBorderBox(
                new RectangleF(block.X, block.Y, block.Width, block.Height),
                padding,
                border,
                markerOffset: block.MarkerOffset);
        }

        foreach (var child in block.Children.OfType<BlockBox>())
        {
            EnsureUsedGeometry(child);
        }
    }

    private static void PrepareInlineLayout(BlockBox block, IInlineLayoutEngine inlineEngine)
    {
        var padding = block.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(block.Style.Borders).Safe();
        var contentLeft = block.X + padding.Left + border.Left;
        var contentTop = block.Y + padding.Top + border.Top;
        var contentWidth = Math.Max(0f, block.Width - padding.Horizontal - border.Horizontal);

        inlineEngine.Layout(
            block,
            new InlineLayoutRequest(
                contentLeft,
                contentTop,
                contentWidth));

        foreach (var child in block.Children.OfType<BlockBox>())
        {
            if (child is InlineBlockBoundaryBox)
            {
                continue;
            }

            if (child.IsAnonymous && child.Children.All(static grandChild => grandChild is InlineBox))
            {
                continue;
            }

            PrepareInlineLayout(child, inlineEngine);
        }
    }

    private static BlockBox CreateListItem(string text, DisplayNode parent)
    {
        var listItem = new BlockBox(DisplayRole.ListItem)
        {
            X = 0,
            Y = 0,
            Width = 500,
            Height = 30,
            Style = new ComputedStyle { FontSizePt = 12 },
            Element = CreateElement("li"),
            Parent = parent
        };

        listItem.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            TextContent = text,
            Style = listItem.Style,
            Parent = listItem
        });

        return listItem;
    }

    private static IEnumerable<BlockFragment> EnumerateBlockFragments(BlockFragment root)
    {
        yield return root;

        foreach (var child in root.Children.OfType<BlockFragment>())
        {
            foreach (var nested in EnumerateBlockFragments(child))
            {
                yield return nested;
            }
        }
    }

    private static AngleSharp.Dom.IElement CreateElement(string tagName)
    {
        return BrowsingContext.New(Configuration.Default)
            .OpenNewAsync().Result.CreateElement(tagName);
    }

    private sealed class SizeBasedTextMeasurer(float widthPerChar) : ITextMeasurer
    {
        private readonly float _widthPerChar = widthPerChar;

        public float MeasureWidth(FontKey font, float sizePt, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            return text.Length * _widthPerChar;
        }

        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt)
        {
            return (sizePt * 0.7f, sizePt * 0.3f);
        }
    }
}
