using System.Drawing;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.LayoutEngine.Test.TestHelpers;
using Moq;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Geometry;

/// <summary>
/// Verifies layout geometry creation, projection, and measurement contracts.
/// </summary>
public sealed class GeometryContractTests
{
    [Fact]
    public void LayoutTableBlock_NegativeLeftMargin_ClampsOrigin()
    {
        var table = new TableBox(DisplayRole.Table)
        {
            Style = new ComputedStyle
            {
                Margin = new Spacing(0f, 0f, 0f, -8f),
                WidthPt = 40f
            }
        };
        var tableEngine = new Mock<ITableLayoutEngine>();
        tableEngine
            .Setup(engine => engine.Layout(table, It.IsAny<float>()))
            .Returns(new TableLayoutResult
            {
                ResolvedWidth = 40f,
                Height = 20f
            });
        var inlineEngine = new Mock<IInlineLayoutEngine>();
        var layoutEngine = new BlockLayoutEngine(
            inlineEngine.Object,
            tableEngine.Object);
        var page = new PageBox
        {
            Size = new SizePt(100f, 200f),
            Margin = new Spacing(0f, 0f, 0f, 10f)
        };

        var tree = layoutEngine.Layout(table, page);

        var laidOutTable = tree.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        laidOutTable.UsedGeometry.ShouldNotBeNull();
        laidOutTable.UsedGeometry.Value.BorderBoxRect.ShouldBe(new RectangleF(10f, 0f, 40f, 20f));
        tableEngine.Verify(engine => engine.Layout(table, 90f), Times.Once);
    }

    [Fact]
    public void FromBorderBox_OversizedPaddingAndBorders_ClampsContentRectToZeroSize()
    {
        var geometry = BoxGeometryFactory.FromBorderBox(
            1f,
            2f,
            10f,
            6f,
            new Spacing(4f, 4f, 4f, 4f),
            new Spacing(3f, 3f, 3f, 3f));

        geometry.BorderBoxRect.ShouldBe(new RectangleF(1f, 2f, 10f, 6f));
        geometry.ContentBoxRect.ShouldBe(new RectangleF(8f, 9f, 0f, 0f));
    }

    [Fact]
    public void MeasureBorderBoxHeight_RepeatedWidths_DoesNotMutateInlineLayout()
    {
        var style = new ComputedStyle();
        var block = new BlockBox(DisplayRole.Block)
        {
            Style = style
        };
        block.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = block,
            Style = style,
            TextContent = "abcd efgh"
        });
        var inlineEngine = new InlineLayoutEngine(
            new FontMetricsProvider(),
            new FakeTextMeasurer(10f, 9f, 3f),
            new DefaultLineHeightStrategy());
        var measurement = new BlockContentMeasurementService(
            inlineEngine,
            new BlockMeasurementService(),
            new ImageLayoutResolver());

        var existingLayout = new InlineLayoutResult(
            [
                new InlineFlowSegmentLayout([], Top: 123f, Height: 45f)
            ],
            TotalHeight: 45f,
            MaxLineWidth: 67f);
        block.InlineLayout = existingLayout;

        var wideHeight = measurement.MeasureBorderBoxHeight(block, 200f, MeasureNoTables);
        block.InlineLayout.ShouldBeSameAs(existingLayout);
        var narrowHeight = measurement.MeasureBorderBoxHeight(block, 25f, MeasureNoTables);

        narrowHeight.ShouldBeGreaterThan(wideHeight);
        block.InlineLayout.ShouldBeSameAs(existingLayout);
    }

    [Fact]
    public async Task Build_InlineBlockBoundary_PreservesInlineFlowFragmentOrder()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  before
                  <span style='display: inline-block; padding: 1pt;'>X</span>
                  after
                </div>
              </body>
            </html>
            """);

        var parent = result.Layout.Pages
            .SelectMany(static page => page.Children)
            .SelectMany(EnumerateFragments)
            .OfType<BlockFragment>()
            .First(fragment =>
                fragment.Children.Count >= 3 &&
                ContainsText(fragment.Children[0], "before") &&
                fragment.Children[1] is BlockFragment { FormattingContext: FormattingContextKind.InlineBlock } &&
                ContainsText(fragment.Children[2], "after"));

        parent.Children[0].ShouldBeOfType<LineBoxFragment>();
        parent.Children[1].ShouldBeOfType<BlockFragment>()
            .FormattingContext.ShouldBe(FormattingContextKind.InlineBlock);
        parent.Children[2].ShouldBeOfType<LineBoxFragment>();
    }

    [Fact]
    public async Task Build_FragmentProjection_DoesNotLinkFragmentGeometryToSourceBox()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0; padding: 2pt;'>alpha</div>
              </body>
            </html>
            """);
        var binding = result.Observer.BlockBindings.First(item =>
            item.Source.UsedGeometry.HasValue &&
            item.Fragment.Rect.Width > 0f &&
            item.Fragment.Rect.Height > 0f);
        var originalFragmentRect = binding.Fragment.Rect;
        var sourceGeometry = binding.Source.UsedGeometry!.Value;

        binding.Source.ApplyLayoutGeometry(BoxGeometryFactory.FromBorderBox(
            sourceGeometry.X + 25f,
            sourceGeometry.Y + 25f,
            sourceGeometry.Width + 5f,
            sourceGeometry.Height + 5f,
            binding.Source.Padding,
            new Spacing()));

        binding.Source.UsedGeometry.Value.BorderBoxRect.ShouldNotBe(originalFragmentRect);
        binding.Fragment.Rect.ShouldBe(originalFragmentRect);
    }

    [Fact]
    public void InlineLayoutEngineMeasure_TextBlock_PreservesInlineLayout()
    {
        var style = new ComputedStyle();
        var block = new BlockBox(DisplayRole.Block)
        {
            Style = style,
            InlineLayout = new InlineLayoutResult(
                [
                    new InlineFlowSegmentLayout([], Top: 40f, Height: 12f)
                ],
                TotalHeight: 12f,
                MaxLineWidth: 30f)
        };
        block.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = block,
            Style = style,
            TextContent = "abcd efgh"
        });
        var originalLayout = block.InlineLayout;
        var engine = new InlineLayoutEngine(
            new FontMetricsProvider(),
            new FakeTextMeasurer(10f, 9f, 3f),
            new DefaultLineHeightStrategy());

        var measured = engine.Measure(block, InlineLayoutRequest.ForMeasurement(25f));

        measured.TotalHeight.ShouldBeGreaterThan(0f);
        measured.MaxLineWidth.ShouldBeLessThanOrEqualTo(25f);
        measured.Segments.ShouldBeEmpty();
        block.InlineLayout.ShouldBeSameAs(originalLayout);
    }

    [Fact]
    public void IInlineLayoutEngine_DefaultMeasure_ThrowsBeforeLayoutCanMutateGeometry()
    {
        var block = new BlockBox(DisplayRole.Block)
        {
            Style = new ComputedStyle()
        };
        IInlineLayoutEngine engine = new MutatingLayoutOnlyInlineEngine();

        Should.Throw<NotSupportedException>(() =>
            engine.Measure(block, InlineLayoutRequest.ForMeasurement(25f)));

        block.InlineLayout.ShouldBeNull();
        block.UsedGeometry.ShouldBeNull();
    }

    [Fact]
    public void UsedGeometry_Transformations_ReturnNewGeometry()
    {
        var geometry = BoxGeometryFactory.FromBorderBox(
            10f,
            20f,
            100f,
            50f,
            new Spacing(2f, 3f, 4f, 5f),
            new Spacing(1f, 1f, 1f, 1f),
            baseline: 30f,
            markerOffset: 7f);

        var translated = geometry.Translate(4f, -6f);
        var resized = geometry.WithBorderWidth(8f);

        geometry.BorderBoxRect.ShouldBe(new RectangleF(10f, 20f, 100f, 50f));
        geometry.ContentBoxRect.ShouldBe(new RectangleF(16f, 23f, 90f, 42f));
        geometry.Baseline.ShouldBe(30f);
        translated.BorderBoxRect.ShouldBe(new RectangleF(14f, 14f, 100f, 50f));
        translated.ContentBoxRect.ShouldBe(new RectangleF(20f, 17f, 90f, 42f));
        translated.Baseline.ShouldBe(24f);
        resized.BorderBoxRect.ShouldBe(new RectangleF(10f, 20f, 8f, 50f));
        resized.ContentBoxRect.ShouldBe(new RectangleF(16f, 23f, 0f, 42f));
    }

    [Fact]
    public void UsedGeometry_InvalidConstructorInput_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new UsedGeometry(
            new RectangleF(float.NaN, 0f, 10f, 10f),
            new RectangleF(0f, 0f, 10f, 10f),
            baseline: null,
            markerOffset: 0f,
            allowsOverflow: false));
    }

    [Theory]
    [InlineData(float.NaN, 0f, 10f, 10f)]
    [InlineData(0f, 0f, -1f, 10f)]
    public void Fragment_InvalidPublishedRect_Throws(
        float x,
        float y,
        float width,
        float height)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new BlockFragment
        {
            Rect = new RectangleF(x, y, width, height)
        });
    }

    [Fact]
    public async Task Build_NestedInlineBlockImage_UsesConfiguredImageProvider()
    {
        var imageProvider = new FixedImageProvider(new SizePx(32d, 16d));
        var layout = await new LayoutBuilderFixture().BuildLayoutAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  <span style='display: inline-block;'>
                    <span style='display: inline-block;'>
                      <img src='nested.png' />
                    </span>
                  </span>
                </div>
              </body>
            </html>
            """,
            GeometryTestHarness.CreateTextMeasurer(),
            new LayoutOptions { PageSize = PaperSizes.A4 },
            imageProvider);

        var image = layout.Pages
            .SelectMany(static page => page.Children)
            .SelectMany(EnumerateFragments)
            .OfType<ImageFragment>()
            .ShouldHaveSingleItem();

        image.Src.ShouldBe("nested.png");
        image.ContentRect.Width.ShouldBe(24f);
        image.ContentRect.Height.ShouldBe(12f);
    }

    [Fact]
    public async Task Build_TopLevelBlockImage_EmitsImageFragmentInsideBlockFragment()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <img src='image.png' width='40' height='20' style='display: block;' />
              </body>
            </html>
            """);

        var block = result.Layout.Pages
            .ShouldHaveSingleItem()
            .Children
            .ShouldHaveSingleItem()
            .ShouldBeOfType<BlockFragment>();

        var image = block.Children.ShouldHaveSingleItem().ShouldBeOfType<ImageFragment>();

        image.ContentRect.Width.ShouldBe(30f, 0.01f);
        image.ContentRect.Height.ShouldBe(15f, 0.01f);
    }

    [Fact]
    public async Task Build_ImageWithCssDimensions_OverridesHtmlAttributes()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <img src='image.png' width='100' height='50' style='display: block; width: 40px; height: 20px; padding: 4px; border: 2px solid black;' />
              </body>
            </html>
            """);

        var block = result.Layout.Pages
            .ShouldHaveSingleItem()
            .Children
            .ShouldHaveSingleItem()
            .ShouldBeOfType<BlockFragment>();
        var image = block.Children.ShouldHaveSingleItem().ShouldBeOfType<ImageFragment>();

        image.ContentRect.Width.ShouldBe(21f, 0.01f);
        image.ContentRect.Height.ShouldBe(15f, 0.01f);
        image.Rect.Width.ShouldBe(30f, 0.01f);
        image.Rect.Height.ShouldBe(24f, 0.01f);
    }

    [Fact]
    public async Task Build_TopLevelRule_EmitsRuleFragmentInsideBlockFragment()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <hr style='display: block; margin: 0; border-top-width: 2pt; border-top-style: solid;' />
              </body>
            </html>
            """);

        var block = result.Layout.Pages
            .ShouldHaveSingleItem()
            .Children
            .ShouldHaveSingleItem()
            .ShouldBeOfType<BlockFragment>();

        block.Children.ShouldHaveSingleItem().ShouldBeOfType<RuleFragment>();
    }

    private static float MeasureNoTables(TableBox table, float availableWidth)
    {
        return 0f;
    }

    private static IEnumerable<Abstractions.Layout.Fragments.Fragment> EnumerateFragments(
        Abstractions.Layout.Fragments.Fragment fragment)
    {
        yield return fragment;

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var nested in EnumerateFragments(child))
            {
                yield return nested;
            }
        }
    }

    private static bool ContainsText(Abstractions.Layout.Fragments.Fragment fragment, string expected)
    {
        return EnumerateFragments(fragment)
            .OfType<LineBoxFragment>()
            .SelectMany(static line => line.Runs)
            .Any(run => run.Text.Contains(expected, StringComparison.Ordinal));
    }

    /// <summary>
    /// Provides deterministic intrinsic dimensions for image geometry tests.
    /// </summary>
    private sealed class FixedImageProvider(SizePx intrinsicSize) : IImageProvider
    {
        public ImageLoadResult Load(string src, string baseDirectory, long maxBytes)
        {
            return new ImageLoadResult
            {
                Src = src,
                Status = ImageLoadStatus.Ok,
                IntrinsicSizePx = intrinsicSize
            };
        }
    }

    /// <summary>
    /// Exposes a mutating layout path for default measurement contract tests.
    /// </summary>
    private sealed class MutatingLayoutOnlyInlineEngine : IInlineLayoutEngine
    {
        public InlineLayoutResult Layout(BlockBox block, InlineLayoutRequest request)
        {
            block.InlineLayout = InlineLayoutResult.Empty;
            block.ApplyLayoutGeometry(BoxGeometryFactory.FromBorderBox(
                1f,
                2f,
                3f,
                4f,
                new Spacing(),
                new Spacing()));
            return InlineLayoutResult.Empty;
        }
    }
}
