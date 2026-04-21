using System.Drawing;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Text;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;
using Shouldly;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test;

public sealed class RegistryExtensibilityTests
{
    [Fact]
    public void Layout_WithCustomBlockStrategy_UsesInjectedStrategy()
    {
        var inlineEngine = new Mock<IInlineLayoutEngine>();
        inlineEngine
            .Setup(engine => engine.Layout(It.IsAny<BlockBox>(), It.IsAny<InlineLayoutRequest>()))
            .Returns(InlineLayoutResult.Empty);

        var strategies = new BlockLayoutStrategyRegistry(
        [
            new BadgeBlockLayoutStrategy(),
            .. BlockLayoutStrategyRegistry.CreateDefaultStrategies()
        ]);

        var root = new BlockBox(DisplayRole.Block)
        {
            Style = new ComputedStyle()
        };
        var badge = new BadgeBlockBox
        {
            Parent = root,
            Style = new ComputedStyle()
        };
        root.Children.Add(badge);

        var engine = new BlockLayoutEngine(
            inlineEngine.Object,
            Mock.Of<ITableLayoutEngine>(),
            Mock.Of<IFloatLayoutEngine>(),
            new BlockFormattingContext(),
            new ImageLayoutResolver(),
            strategies);

        var result = engine.Layout(root, CreatePage());

        var laidOut = result.Blocks.ShouldHaveSingleItem();
        laidOut.ShouldBeSameAs(badge);
        laidOut.UsedGeometry.ShouldNotBeNull();
        laidOut.Width.ShouldBe(33f);
        laidOut.Height.ShouldBe(11f);
        laidOut.UsedGeometry!.Value.BorderBoxRect.ShouldBe(new RectangleF(0f, 0f, 33f, 11f));
    }

    [Fact]
    public void Layout_WithCustomInlineNodeMeasurer_UsesInjectedMeasurerBeforeDefaultTextHandling()
    {
        var block = new BlockBox(DisplayRole.Block)
        {
            Style = new ComputedStyle { FontSizePt = 12f }
        };
        block.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = block,
            TextContent = "[badge]",
            Style = block.Style
        });

        var measurers = new InlineNodeMeasurerRegistry(
        [
            new BadgeInlineNodeMeasurer(),
            .. InlineNodeMeasurerRegistry.CreateDefaultMeasurers()
        ]);
        var engine = new InlineLayoutEngine(
            new FontMetricsProvider(),
            new FakeTextMeasurer(4f, 8f, 2f),
            new DefaultLineHeightStrategy(),
            new BlockFormattingContext(),
            measurers);

        var result = engine.Layout(block, new InlineLayoutRequest(0f, 0f, 120f));

        var textItem = result.Segments.ShouldHaveSingleItem()
            .Lines.ShouldHaveSingleItem()
            .Items.ShouldHaveSingleItem()
            .ShouldBeOfType<InlineTextItemLayout>();
        textItem.Runs.Select(static run => run.Text).ShouldBe(["custom-badge"]);
    }

    [Fact]
    public void Build_WithCustomBlockFragmentAdapter_UsesInjectedAdapter()
    {
        var block = new BadgeBlockBox
        {
            Style = new ComputedStyle(),
            UsedGeometry = UsedGeometry.FromBorderBox(new RectangleF(10f, 20f, 40f, 15f), new Spacing(), new Spacing())
        };

        var tree = new BoxTree();
        tree.Blocks.Add(block);

        var adapters = new FragmentAdapterRegistry(
        [
            new BadgeBlockFragmentAdapter(),
            .. FragmentAdapterRegistry.CreateDefaultBlockAdapters()
        ],
            FragmentAdapterRegistry.CreateDefaultSpecialAdapters());

        var fragments = new FragmentBuilder([], adapters).Build(tree, CreateFragmentContext());

        fragments.Blocks.ShouldHaveSingleItem().ShouldBeOfType<BadgeBlockFragment>();
    }

    [Fact]
    public void Build_WithCustomSpecialFragmentAdapter_UsesInjectedAdapter()
    {
        var root = new BlockBox(DisplayRole.Block)
        {
            Style = new ComputedStyle(),
            UsedGeometry = UsedGeometry.FromBorderBox(new RectangleF(0f, 0f, 100f, 30f), new Spacing(), new Spacing()),
            InlineLayout = InlineLayoutResult.Empty
        };
        var rule = new RuleBox(DisplayRole.Block)
        {
            Parent = root,
            Style = new ComputedStyle(),
            UsedGeometry = UsedGeometry.FromBorderBox(new RectangleF(0f, 0f, 100f, 2f), new Spacing(), new Spacing())
        };
        root.Children.Add(rule);

        var tree = new BoxTree();
        tree.Blocks.Add(root);

        var adapters = new FragmentAdapterRegistry(
            FragmentAdapterRegistry.CreateDefaultBlockAdapters(),
        [
            new BadgeSpecialFragmentAdapter(),
            .. FragmentAdapterRegistry.CreateDefaultSpecialAdapters()
        ]);

        var fragments = new FragmentBuilder([], adapters).Build(tree, CreateFragmentContext());
        var rootFragment = fragments.Blocks.ShouldHaveSingleItem();

        rootFragment.Children.Any(static child => child is BadgeSpecialFragment).ShouldBeTrue();
    }

    private static PageBox CreatePage()
    {
        return new PageBox
        {
            Margin = new Spacing(),
            Size = new SizePt(200f, 300f)
        };
    }

    private static FragmentBuildContext CreateFragmentContext()
    {
        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(source => source.Resolve(It.IsAny<FontKey>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));

        return new FragmentBuildContext(
            new NoopImageProvider(),
            Directory.GetCurrentDirectory(),
            10 * 1024 * 1024,
            new FakeTextMeasurer(4f, 8f, 2f),
            fontSource.Object,
            new BlockFormattingContext());
    }

    private sealed class BadgeBlockBox() : BlockBox(DisplayRole.Block);

    private sealed class BadgeBlockLayoutStrategy : IBlockLayoutStrategy
    {
        public bool CanLayout(BlockBox node) => node is BadgeBlockBox;

        public BlockBox Layout(BlockLayoutEngine engine, BlockBox node, BlockLayoutRequest request)
        {
            node.Margin = node.Style.Margin.Safe();
            node.Padding = node.Style.Padding.Safe();
            node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
            node.UsedGeometry = UsedGeometry.FromBorderBox(
                new RectangleF(request.ContentX, request.CursorY + request.CollapsedTopMargin, 33f, 11f),
                node.Padding,
                Spacing.FromBorderEdges(node.Style.Borders).Safe(),
                markerOffset: node.MarkerOffset);
            return node;
        }
    }

    private sealed class BadgeInlineNodeMeasurer : IInlineNodeMeasurer
    {
        public bool TryMeasure(
            DisplayNode node,
            InlineMeasurementContext context,
            ICollection<TextRunInput> runs,
            ref int nextRunId)
        {
            if (node is not InlineBox inline || !string.Equals(inline.TextContent, "[badge]", StringComparison.Ordinal))
            {
                return false;
            }

            var synthetic = new InlineBox(DisplayRole.Inline)
            {
                Parent = inline.Parent,
                Style = inline.Style,
                TextContent = "custom-badge"
            };

            return context.TryAppendTextRun(synthetic, runs, ref nextRunId);
        }
    }

    private sealed class BadgeBlockFragment : BlockFragment;

    private sealed class BadgeBlockFragmentAdapter : IBlockFragmentAdapter
    {
        public bool CanCreate(BlockBox source) => source is BadgeBlockBox;

        public BlockFragment Create(BlockBox source, FragmentBuildState state)
        {
            return new BadgeBlockFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = source.UsedGeometry!.Value.BorderBoxRect,
                Style = new VisualStyle(),
                DisplayRole = FragmentDisplayRole.Block,
                FormattingContext = FormattingContextKind.Block
            };
        }
    }

    private sealed class BadgeSpecialFragment : LayoutFragment;

    private sealed class BadgeSpecialFragmentAdapter : ISpecialFragmentAdapter
    {
        public bool CanCreate(DisplayNode source) => source is RuleBox;

        public LayoutFragment Create(DisplayNode source, FragmentBuildState state)
        {
            var rule = (RuleBox)source;
            return new BadgeSpecialFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = rule.UsedGeometry!.Value.BorderBoxRect,
                Style = new VisualStyle()
            };
        }
    }
}
