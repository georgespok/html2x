using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.LayoutEngine.Pagination;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Geometry;

/// <summary>
///     Verifies layout geometry creation, projection, and measurement contracts.
/// </summary>
public sealed class GeometryContractTests
{
    [Fact]
    public void LayoutTableBlock_NegativeLeftMargin_ClampsOrigin()
    {
        var table = new TableBox(BoxRole.Table)
        {
            Style = new()
            {
                Margin = new(0f, 0f, 0f, -8f),
                WidthPt = 40f
            }
        };
        var inlineEngine = new InlineFlowLayout();
        var imageResolver = new ImageSizingRules();
        var layoutEngine = new BlockBoxLayout(
            inlineEngine,
            new(inlineEngine, imageResolver),
            new(),
            imageResolver);
        var page = new PageBox
        {
            Size = new(100f, 200f),
            Margin = new(0f, 0f, 0f, 10f)
        };

        _ = PublishedLayoutTestResolver.Resolve(layoutEngine, table, page);

        table.UsedGeometry.ShouldNotBeNull();
        table.UsedGeometry.Value.BorderBoxRect.ShouldBe(new(10f, 0f, 40f, 0f));
    }

    [Fact]
    public void FromBorderBox_OversizedPaddingAndBorders_ClampsContentRectToZeroSize()
    {
        var geometry = UsedGeometryRules.FromBorderBox(
            1f,
            2f,
            10f,
            6f,
            new(4f, 4f, 4f, 4f),
            new(3f, 3f, 3f, 3f));

        geometry.BorderBoxRect.ShouldBe(new(1f, 2f, 10f, 6f));
        geometry.ContentBoxRect.ShouldBe(new(8f, 9f, 0f, 0f));
    }

    [Fact]
    public void MeasureBorderBoxHeight_RepeatedWidths_DoesNotMutateInlineLayout()
    {
        var style = new ComputedStyle();
        var block = new BlockBox(BoxRole.Block)
        {
            Style = style
        };
        block.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = block,
            Style = style,
            TextContent = "abcd efgh"
        });
        var inlineEngine = new InlineFlowLayout(
            new FontMetricsProvider(),
            new FakeTextMeasurer(10f, 9f, 3f),
            new DefaultLineHeightStrategy());
        var heightCalculator = new BlockContentHeightMeasurement(
            new(
                inlineEngine,
                new(),
                new ImageSizingRules()));

        var existingLayout = new InlineLayoutResult(
            [
                new([], 123f, 45f)
            ],
            45f,
            67f);
        block.InlineLayout = existingLayout;

        var wideHeight = heightCalculator.MeasureBorderBoxHeight(block, 200f, MeasureNoTables);
        block.InlineLayout.ShouldBeSameAs(existingLayout);
        var narrowHeight = heightCalculator.MeasureBorderBoxHeight(block, 25f, MeasureNoTables);

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
    public async Task Build_FragmentProjection_UsesPublishedGeometryValue()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0; padding: 2pt;'>alpha</div>
              </body>
            </html>
            """);
        var source = result.PublishedLayout.Blocks.First(block =>
            block.Geometry.Width > 0f &&
            block.Geometry.Height > 0f);
        var fragment = result.Fragments.Blocks.First(block =>
            block.Rect.Width > 0f &&
            block.Rect.Height > 0f);

        fragment.Rect.ShouldBe(source.Geometry.BorderBoxRect);
    }

    [Fact]
    public void InlineFlowLayoutMeasure_TextBlock_PreservesInlineLayout()
    {
        var style = new ComputedStyle();
        var block = new BlockBox(BoxRole.Block)
        {
            Style = style,
            InlineLayout = new(
                [
                    new([], 40f, 12f)
                ],
                12f,
                30f)
        };
        block.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = block,
            Style = style,
            TextContent = "abcd efgh"
        });
        var originalLayout = block.InlineLayout;
        var engine = new InlineFlowLayout(
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
    public void InlineFlowLayoutMeasure_InlineBlockImage_SkipsImageFacts()
    {
        var style = new ComputedStyle();
        var root = new BlockBox(BoxRole.Block)
        {
            Style = style
        };
        var inline = new InlineBox(BoxRole.InlineBlock)
        {
            Parent = root,
            Style = style
        };
        var image = new ImageBox(BoxRole.Block)
        {
            Parent = inline,
            Style = style,
            Src = "before.png",
            AuthoredSizePx = new(1d, 2d),
            IntrinsicSizePx = new(3d, 4d),
            Status = ImageLoadStatus.Oversize
        };
        image.ApplyLayoutGeometry(UsedGeometryRules.FromBorderBox(
            1f,
            2f,
            3f,
            4f,
            new(),
            new()));
        var originalGeometry = image.UsedGeometry;

        inline.Children.Add(image);
        root.Children.Add(inline);

        var imageResolver = new ImageSizingRules(new()
        {
            ImageMetadataResolver = new FixedImageMetadataResolver(new(40d, 20d))
        });
        var engine = new InlineFlowLayout(
            new FontMetricsProvider(),
            new FakeTextMeasurer(10f, 9f, 3f),
            new DefaultLineHeightStrategy(),
            new(),
            imageResolver);

        var measured = engine.Measure(root, InlineLayoutRequest.ForMeasurement(100f));

        measured.TotalHeight.ShouldBeGreaterThan(0f);
        measured.Segments.ShouldBeEmpty();
        image.UsedGeometry.ShouldBe(originalGeometry);
        image.Src.ShouldBe("before.png");
        image.AuthoredSizePx.ShouldBe(new(1d, 2d));
        image.IntrinsicSizePx.ShouldBe(new(3d, 4d));
        image.IsMissing.ShouldBeFalse();
        image.IsOversize.ShouldBeTrue();
    }

    [Fact]
    public void UsedGeometry_Transformations_ReturnNewGeometry()
    {
        var geometry = UsedGeometryRules.FromBorderBox(
            10f,
            20f,
            100f,
            50f,
            new(2f, 3f, 4f, 5f),
            new(1f, 1f, 1f, 1f),
            30f,
            7f);

        var translated = geometry.Translate(4f, -6f);
        var resized = geometry.WithBorderWidth(8f);

        geometry.BorderBoxRect.ShouldBe(new(10f, 20f, 100f, 50f));
        geometry.ContentBoxRect.ShouldBe(new(16f, 23f, 90f, 42f));
        geometry.Baseline.ShouldBe(30f);
        translated.BorderBoxRect.ShouldBe(new(14f, 14f, 100f, 50f));
        translated.ContentBoxRect.ShouldBe(new(20f, 17f, 90f, 42f));
        translated.Baseline.ShouldBe(24f);
        resized.BorderBoxRect.ShouldBe(new(10f, 20f, 8f, 50f));
        resized.ContentBoxRect.ShouldBe(new(16f, 23f, 0f, 42f));
    }

    [Fact]
    public void UsedGeometry_InvalidConstructorInput_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new UsedGeometry(
            new(float.NaN, 0f, 10f, 10f),
            new(0f, 0f, 10f, 10f),
            null,
            0f,
            false));
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
            Rect = new(x, y, width, height)
        });
    }

    [Theory]
    [InlineData(float.NaN, 10f, 20f, 9f, 3f)]
    [InlineData(-1f, 10f, 20f, 9f, 3f)]
    [InlineData(12f, float.NaN, 20f, 9f, 3f)]
    [InlineData(12f, 10f, -1f, 9f, 3f)]
    [InlineData(12f, 10f, 20f, -1f, 3f)]
    [InlineData(12f, 10f, 20f, 9f, -1f)]
    public void TextRun_InvalidPublishedMetric_Throws(
        float fontSize,
        float originX,
        float advanceWidth,
        float ascent,
        float descent)
    {
        var font = new FontKey("Arial", FontWeight.W400, FontStyle.Normal);

        Should.Throw<ArgumentOutOfRangeException>(() => new TextRun(
            "x",
            font,
            fontSize,
            new(originX, 20f),
            advanceWidth,
            ascent,
            descent));
    }

    [Theory]
    [InlineData(float.NaN, 12f)]
    [InlineData(10f, float.NaN)]
    [InlineData(10f, -1f)]
    public void LineBoxFragment_InvalidPublishedMetric_Throws(float baselineY, float lineHeight)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new LineBoxFragment
        {
            Rect = new(0f, 0f, 10f, 12f),
            BaselineY = baselineY,
            LineHeight = lineHeight
        });
    }

    [Fact]
    public void Layout_NegativePageMargins_NormalizesContentOrigin()
    {
        var root = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        var block = new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = new()
        };
        root.Children.Add(block);
        var inlineEngine = new InlineFlowLayout();
        var imageResolver = new ImageSizingRules();
        var layoutEngine = new BlockBoxLayout(
            inlineEngine,
            new(inlineEngine, imageResolver),
            new(),
            imageResolver);
        var page = new PageBox
        {
            Size = new(100f, 200f),
            Margin = new(-5f, -10f, -15f, -20f)
        };

        var published = PublishedLayoutTestResolver.Resolve(layoutEngine, root, page);

        var laidOutBlock = published.Blocks.ShouldHaveSingleItem();
        laidOutBlock.Geometry.BorderBoxRect.X.ShouldBe(0f);
        laidOutBlock.Geometry.BorderBoxRect.Y.ShouldBe(0f);
        laidOutBlock.Geometry.BorderBoxRect.Width.ShouldBe(100f);
    }

    [Fact]
    public void Paginate_NegativePageMargins_NormalizesContentBounds()
    {
        var result = new LayoutPaginator().Paginate(
            [],
            new()
            {
                PageSize = new(100f, 200f),
                Margin = new(-5f, -10f, -15f, -20f)
            });

        var page = result.AuditPages.ShouldHaveSingleItem();
        page.ContentTop.ShouldBe(0f);
        page.ContentBottom.ShouldBe(200f);
    }

    [Fact]
    public async Task Build_NestedInlineBlockImage_UsesConfiguredImageMetadataResolver()
    {
        var imageMetadataResolver = new FixedImageMetadataResolver(new(32d, 16d));
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
            new() { PageSize = PaperSizes.A4 },
            imageMetadataResolver);

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

        image.ContentRect.Width.ShouldBe(30f, 0.01f);
        image.ContentRect.Height.ShouldBe(15f, 0.01f);
        image.Rect.Width.ShouldBe(39f, 0.01f);
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

    private static float MeasureNoTables(TableBox table, float availableWidth) => 0f;

    private static IEnumerable<Fragment> EnumerateFragments(
        Fragment fragment)
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

    private static bool ContainsText(Fragment fragment, string expected)
    {
        return EnumerateFragments(fragment)
            .OfType<LineBoxFragment>()
            .SelectMany(static line => line.Runs)
            .Any(run => run.Text.Contains(expected, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Provides deterministic intrinsic dimensions for image geometry tests.
    /// </summary>
    private sealed class FixedImageMetadataResolver(SizePx intrinsicSize) : IImageMetadataResolver
    {
        public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes) =>
            new()
            {
                Src = src,
                Status = ImageLoadStatus.Ok,
                IntrinsicSizePx = intrinsicSize
            };
    }
}