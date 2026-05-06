using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.Text;
using Shouldly;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;
using static Html2x.LayoutEngine.Geometry.Test.Diagnostics.DiagnosticFieldAssertions;

namespace Html2x.LayoutEngine.Geometry.Test.Display;

public class BlockFlowTests
{
    private static readonly LayoutBuilderFixture Fixture = new();

    [Fact]
    public async Task BlockStacking_UsesCollapsedMarginsAndIncludesPaddingInHeight()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='margin-bottom: 10pt; padding-top: 4pt; border-top: 2pt solid #000; height: 20pt;'>A</div>
                <div style='margin-top: 6pt; height: 10pt;'>B</div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(2);

        var first = (BlockFragment)layout.Pages[0].Children[0];
        var second = (BlockFragment)layout.Pages[0].Children[1];

        var expectedFirstHeight = 20f + 4f + 2f;
        first.Rect.Height.ShouldBe(expectedFirstHeight, 0.5f);

        var expectedGap = 10f; // collapsed max(10pt, 6pt)
        var expectedSecondY = first.Rect.Y + first.Rect.Height + expectedGap;
        second.Rect.Y.ShouldBe(expectedSecondY, 0.5f);
    }

    [Fact]
    public async Task AdjacentBlocks_CollapseMarginsToMaxValue()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='margin-bottom: 20pt; height: 10pt;'>First</div>
                <div style='margin-top: 6pt; height: 10pt;'>Second</div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var first = (BlockFragment)layout.Pages[0].Children[0];
        var second = (BlockFragment)layout.Pages[0].Children[1];

        var expectedGap = 20f;
        var expectedSecondY = first.Rect.Y + first.Rect.Height + expectedGap;

        second.Rect.Y.ShouldBe(expectedSecondY, 0.5f);
    }

    [Fact]
    public async Task ParentAndChildMargins_DoNotCollapse()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='margin-top: 12pt;'>
                  <p style='margin-top: 8pt; height: 10pt;'>Child</p>
                </div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var parent = (BlockFragment)layout.Pages[0].Children[0];
        var child = parent.Children.OfType<BlockFragment>().First();

        var expectedChildY = parent.Rect.Y + 8f;
        child.Rect.Y.ShouldBe(expectedChildY, 0.5f);
    }

    [Fact]
    public async Task MarginCollapse_EmitsDiagnosticsRecord()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='margin-bottom: 20pt; height: 10pt;'>First</div>
                <div style='margin-top: 6pt; height: 10pt;'>Second</div>
              </body>
            </html>";

        var diagnosticsSink = new RecordingDiagnosticsSink();

        var layoutBuilder = CreateLayoutBuilder(CreateLinearMeasurer(10f));
        _ = await layoutBuilder.BuildAsync(html, new() { PageSize = PaperSizes.A4 }, diagnosticsSink);

        var marginEvents = diagnosticsSink.Records
            .Where(e => e.Name == "layout/margin-collapse")
            .ToList();

        marginEvents.ShouldNotBeEmpty();
        var match = marginEvents.FirstOrDefault(e =>
            Math.Abs(NumberField(e, "previousBottomMargin") - 20f) < 0.01f &&
            Math.Abs(NumberField(e, "nextTopMargin") - 6f) < 0.01f &&
            Math.Abs(NumberField(e, "collapsedTopMargin") - 20f) < 0.01f);

        match.ShouldNotBeNull();
    }

    [Fact]
    public async Task NegativeMargins_ClampToParentContentBox()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='margin-bottom: -20pt; height: 0;'>First</div>
                <div style='margin-top: -30pt; margin-left: -25pt; height: 10pt;'>Second</div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var second = (BlockFragment)layout.Pages[0].Children[1];

        second.Rect.X.ShouldBeGreaterThanOrEqualTo(0f);
        second.Rect.Y.ShouldBeGreaterThanOrEqualTo(0f);
    }

    [Fact]
    public async Task PaddingAndBorder_AffectBlockHeight()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='height: 12pt; padding-top: 5pt; padding-bottom: 7pt; border-top: 2pt solid #000; border-bottom: 3pt solid #000;'>Box</div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var block = (BlockFragment)layout.Pages[0].Children[0];
        var expectedHeight = 12f + 5f + 7f + 2f + 3f;

        block.Rect.Height.ShouldBe(expectedHeight, 0.5f);
    }

    [Fact]
    public async Task MixedInlineAndBlock_CreatesBlockToInlineBoundary()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div>
                  Inline text
                  <div style='height: 10pt;'>Block</div>
                </div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var container = (BlockFragment)layout.Pages[0].Children[0];
        var childOrder = container.Children
            .Select(child => child switch
            {
                LineBoxFragment line when line.Runs.Any(run =>
                    run.Text.Contains("Inline", StringComparison.OrdinalIgnoreCase)) => "Inline",
                BlockFragment block when block.Children.OfType<LineBoxFragment>()
                    .SelectMany(line => line.Runs)
                    .Any(run => run.Text.Contains("Block", StringComparison.OrdinalIgnoreCase)) => "Block",
                _ => null
            })
            .Where(static label => label is not null)
            .Cast<string>()
            .ToList();

        childOrder.ShouldBe(["Inline", "Block"]);
    }

    [Fact]
    public async Task MixedInlineBlockInline_FlushesInlineFlowAroundBlockChild()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div>
                  before
                  <p style='height: 10pt;'>block</p>
                  after
                </div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var container = (BlockFragment)layout.Pages[0].Children[0];
        var childOrder = container.Children
            .Select(ResolveDirectChildLabel)
            .Where(static label => label is not null)
            .Cast<string>()
            .ToList();

        childOrder.ShouldBe(["before", "block", "after"]);
    }

    [Fact]
    public void NormalizeChildrenForBlock_MixedInlineFlow_PreservesChildOrder()
    {
        var prefixIdentity = CreateContentSourceIdentity(
            10,
            20,
            "body[0]/div[0]/text[0]",
            "div");
        var inlineBlockIdentity = CreateNodeSourceIdentity(
            11,
            "body[0]/div[0]/span[0]",
            "span");
        var inlineBlockContentIdentity = inlineBlockIdentity.AsGenerated(
            GeometryGeneratedSourceKind.InlineBlockContent);
        var suffixIdentity = CreateContentSourceIdentity(
            10,
            21,
            "body[0]/div[0]/text[1]",
            "div");
        var row = new BlockBox(BoxRole.Block)
        {
            IsInlineBlockContext = true,
            Style = new()
        };

        row.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = row,
            TextContent = "Prefix text",
            Style = new(),
            SourceIdentity = prefixIdentity
        });

        var inlineBlock = new InlineBox(BoxRole.InlineBlock)
        {
            Parent = row,
            Style = new(),
            SourceIdentity = inlineBlockIdentity
        };

        var inlineBlockContent = new BlockBox(BoxRole.Block)
        {
            Parent = inlineBlock,
            IsAnonymous = true,
            IsInlineBlockContext = true,
            Style = new(),
            SourceIdentity = inlineBlockContentIdentity
        };

        inlineBlockContent.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = inlineBlockContent,
            TextContent = "Alpha inline-block",
            Style = new()
        });

        inlineBlock.Children.Add(inlineBlockContent);
        row.Children.Add(inlineBlock);

        row.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = row,
            TextContent = "suffix text",
            Style = new(),
            SourceIdentity = suffixIdentity
        });

        BlockFlowNormalization.NormalizeChildrenForBlock(row);

        row.Children.Count.ShouldBe(3);

        var leadingAnonymous = row.Children[0].ShouldBeOfType<BlockBox>();
        var boundaryBlock = row.Children[1].ShouldBeOfType<InlineBlockBoundaryBox>();
        var trailingAnonymous = row.Children[2].ShouldBeOfType<BlockBox>();

        leadingAnonymous.IsAnonymous.ShouldBeTrue();
        boundaryBlock.IsAnonymous.ShouldBeTrue();
        boundaryBlock.IsInlineBlockContext.ShouldBeTrue();
        boundaryBlock.SourceInline.ShouldBeSameAs(inlineBlock);
        boundaryBlock.SourceContentBox.ShouldBeSameAs(inlineBlockContent);
        trailingAnonymous.IsAnonymous.ShouldBeTrue();
        leadingAnonymous.SourceIdentity.GeneratedKind.ShouldBe(GeometryGeneratedSourceKind.AnonymousBlock);
        leadingAnonymous.SourceIdentity.ContentId.ShouldBe(prefixIdentity.ContentId);
        leadingAnonymous.SourceIdentity.SourcePath.ShouldBe($"{prefixIdentity.SourcePath}::anonymous-block");
        boundaryBlock.SourceIdentity.GeneratedKind.ShouldBe(GeometryGeneratedSourceKind.InlineBlockBoundary);
        boundaryBlock.SourceIdentity.NodeId.ShouldBe(inlineBlockContentIdentity.NodeId);
        boundaryBlock.SourceIdentity.SourcePath.ShouldBe(
            $"{inlineBlockContentIdentity.SourcePath}::inline-block-boundary");
        trailingAnonymous.SourceIdentity.GeneratedKind.ShouldBe(GeometryGeneratedSourceKind.AnonymousBlock);
        trailingAnonymous.SourceIdentity.ContentId.ShouldBe(suffixIdentity.ContentId);
        trailingAnonymous.SourceIdentity.SourcePath.ShouldBe($"{suffixIdentity.SourcePath}::anonymous-block");

        ExtractInlineText(leadingAnonymous).ShouldBe(["Prefix text"]);
        ExtractInlineText(boundaryBlock).ShouldBe(["Alpha inline-block"]);
        ExtractInlineText(trailingAnonymous).ShouldBe(["suffix text"]);
    }

    [Fact]
    public void NormalizeChildrenForBlock_ClonedInlineFlow_CopiesInlineMetadata()
    {
        var sourceIdentity = CreateContentSourceIdentity(
            12,
            22,
            "body[0]/div[0]/text[2]",
            "div");
        var parent = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        var inline = new InlineBox(BoxRole.Inline)
        {
            Parent = parent,
            TextContent = "metadata",
            Style = new(),
            Width = 25f,
            Height = 8f,
            BaselineOffset = 6f,
            SourceIdentity = sourceIdentity
        };
        parent.Children.Add(inline);
        parent.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = parent,
            Style = new()
        });

        BlockFlowNormalization.NormalizeChildrenForBlock(parent);

        var anonymous = parent.Children[0].ShouldBeOfType<BlockBox>();
        var clonedInline = anonymous.Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>();

        clonedInline.ShouldNotBeSameAs(inline);
        clonedInline.Parent.ShouldBeSameAs(anonymous);
        clonedInline.TextContent.ShouldBe("metadata");
        clonedInline.Width.ShouldBe(25f);
        clonedInline.Height.ShouldBe(8f);
        clonedInline.BaselineOffset.ShouldBe(6f);
        clonedInline.SourceIdentity.ShouldBe(sourceIdentity);
    }

    [Fact]
    public async Task InlineBlockInternalBlocks_ApplySpacingAndBoundarySemantics()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 400pt;'>
                  <span style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>
                    <div style='height: 10pt; padding-bottom: 5pt; border-bottom: 3pt solid #000; margin-bottom: 12pt;'>First</div>
                    <div style='height: 8pt; margin-top: 4pt;'>Second</div>
                  </span>
                </div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));
        var root = (BlockFragment)layout.Pages[0].Children[0];
        var inlineBlock = EnumerateFragments(root)
            .OfType<BlockFragment>()
            .FirstOrDefault(fragment =>
                fragment.Style?.Borders?.HasAny == true &&
                fragment.Children.OfType<LineBoxFragment>()
                    .SelectMany(line => line.Runs)
                    .Any(run => run.Text.Contains("First", StringComparison.OrdinalIgnoreCase)));

        inlineBlock.ShouldNotBeNull();

        const float expectedFirstBlockHeight = 10f + 5f + 3f;
        const float expectedCollapsedGap = 12f;
        const float expectedSecondBlockHeight = 8f;
        const float expectedOuterPaddingAndBorder = 2f + 2f + 1f + 1f;
        var expectedTotalHeight = expectedFirstBlockHeight + expectedCollapsedGap + expectedSecondBlockHeight +
                                  expectedOuterPaddingAndBorder;

        inlineBlock.Rect.Height.ShouldBe(expectedTotalHeight, 0.5f);

        var lines = inlineBlock.Children.OfType<LineBoxFragment>().ToList();
        lines.Count.ShouldBeGreaterThanOrEqualTo(2);

        var firstLine = lines.First(line =>
            line.Runs.Any(run => run.Text.Contains("First", StringComparison.OrdinalIgnoreCase)));
        var secondLine = lines.First(line =>
            line.Runs.Any(run => run.Text.Contains("Second", StringComparison.OrdinalIgnoreCase)));

        secondLine.Rect.Y.ShouldBeGreaterThan(firstLine.Rect.Y);
        secondLine.Rect.Y.ShouldBeGreaterThanOrEqualTo(firstLine.Rect.Bottom - 0.1f);
    }

    [Fact]
    public async Task UnsupportedStructures_OutsideInlineBlock_DoNotTriggerFailFast()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <table>
                  <tr><td>top-level table</td></tr>
                </table>
              </body>
            </html>";

        var diagnosticsSink = new RecordingDiagnosticsSink();

        var layoutBuilder = CreateLayoutBuilder(CreateLinearMeasurer(10f));
        var layout = await layoutBuilder.BuildAsync(html, new() { PageSize = PaperSizes.A4 }, diagnosticsSink);

        layout.Pages.Count.ShouldBe(1);
        diagnosticsSink.Records
            .Any(e => e.Name == "layout/inline-block/unsupported-structure")
            .ShouldBeFalse();
    }

    [Fact]
    public async Task TopLevelAndInlineBlockInternalFlows_ProduceEquivalentBlockMetrics()
    {
        const string topLevelHtml = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 300pt; border: 1pt solid #000; padding: 2pt;'>
                  <div style='height: 10pt; padding-bottom: 5pt; border-bottom: 3pt solid #000; margin-bottom: 12pt;'>First</div>
                  <div style='height: 8pt; margin-top: 4pt;'>Second</div>
                </div>
              </body>
            </html>";

        const string inlineBlockHtml = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 300pt;'>
                  <span style='display: inline-block; width: 300pt; border: 1pt solid #000; padding: 2pt;'>
                    <div style='height: 10pt; padding-bottom: 5pt; border-bottom: 3pt solid #000; margin-bottom: 12pt;'>First</div>
                    <div style='height: 8pt; margin-top: 4pt;'>Second</div>
                  </span>
                </div>
              </body>
            </html>";

        var topLevelLayout = await BuildLayoutAsync(topLevelHtml, CreateLinearMeasurer(10f));
        var inlineBlockLayout = await BuildLayoutAsync(inlineBlockHtml, CreateLinearMeasurer(10f));

        var topLevelContainer =
            FindContainerWithTexts((BlockFragment)topLevelLayout.Pages[0].Children[0], "First", "Second");
        var inlineBlockContainer =
            FindContainerWithTexts((BlockFragment)inlineBlockLayout.Pages[0].Children[0], "First", "Second");

        topLevelContainer.ShouldNotBeNull();
        inlineBlockContainer.ShouldNotBeNull();

        var topLevelMetrics = ExtractTwoBlockMetrics(topLevelContainer);
        var inlineBlockMetrics = ExtractTwoBlockMetrics(inlineBlockContainer);

        inlineBlockMetrics.TotalHeight.ShouldBe(topLevelMetrics.TotalHeight, 0.1f);
        inlineBlockMetrics.FirstBlockHeight.ShouldBe(topLevelMetrics.FirstBlockHeight, 0.1f);
        inlineBlockMetrics.SecondBlockHeight.ShouldBe(topLevelMetrics.SecondBlockHeight, 0.1f);
    }

    [Fact]
    public void BlockMeasurementAndLayout_ShareCollapsedMarginDiagnostics()
    {
        var diagnosticsSink = new RecordingDiagnosticsSink();
        var formattingContext = new BlockContentExtentMeasurement();
        var sizingRules = new BlockSizingRules(formattingContext.MarginCollapseRules);
        var imageResolver = new ImageSizingRules();
        var inlineEngine = new InlineFlowLayout(
            new FontMetricsProvider(),
            CreateLinearMeasurer(10f),
            new DefaultLineHeightStrategy(),
            formattingContext,
            imageResolver);
        var tableGridLayout = new TableGridLayout(inlineEngine, imageResolver);

        var container = new BlockBox(BoxRole.Block)
        {
            Style = new()
            {
                WidthPt = 300f
            }
        };

        container.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = container,
            Style = new()
            {
                HeightPt = 10f,
                Margin = new(0f, 0f, 12f, 0f)
            }
        });

        container.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = container,
            Style = new()
            {
                HeightPt = 8f,
                Margin = new(6f, 0f, 0f, 0f)
            }
        });

        var root = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        root.Children.Add(container);

        var engine = new BlockBoxLayout(
            inlineEngine,
            tableGridLayout,
            formattingContext,
            imageResolver,
            diagnosticsSink);

        _ = PublishedLayoutTestResolver.Resolve(engine, root, new()
        {
            Margin = new(),
            Size = new(400f, 400f)
        });

        var laidOutContainer = root.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();
        var measuredHeight = sizingRules.MeasureStackedChildBlocks(
            laidOutContainer.Children,
            300f,
            static (block, _) => block.UsedGeometry?.Height ?? 0f,
            static (table, _) => table.UsedGeometry?.Height ?? 0f,
            diagnosticsSink);

        measuredHeight.ShouldBe(laidOutContainer.UsedGeometry.ShouldNotBeNull().Height, 0.1f);

        diagnosticsSink.Records
            .Where(static e => e.Name == "layout/margin-collapse")
            .Any(payload =>
                StringField(payload, "consumer") == "BlockLayoutEngine" &&
                StringField(payload, "owner") == "BlockFormattingContext" &&
                StringField(payload, "formattingContext") == nameof(FormattingContextKind.Block) &&
                Math.Abs(NumberField(payload, "collapsedTopMargin") - 12f) < 0.01f)
            .ShouldBeTrue();

        diagnosticsSink.Records
            .Where(static e => e.Name == "layout/margin-collapse")
            .Any(payload =>
                StringField(payload, "consumer") == nameof(BlockSizingRules) &&
                StringField(payload, "owner") == "BlockFormattingContext" &&
                StringField(payload, "formattingContext") == nameof(FormattingContextKind.Block) &&
                Math.Abs(NumberField(payload, "collapsedTopMargin") - 12f) < 0.01f)
            .ShouldBeTrue();
    }

    private static async Task<HtmlLayout> BuildLayoutAsync(string html, ITextMeasurer textMeasurer) =>
        await Fixture.BuildLayoutAsync(html, textMeasurer, new()
        {
            PageSize = PaperSizes.A4
        });

    private static LayoutBuilder CreateLayoutBuilder(ITextMeasurer textMeasurer) =>
        new(
            textMeasurer,
            new NoopImageMetadataResolver());

    private static ITextMeasurer CreateLinearMeasurer(float widthPerChar) => new FakeTextMeasurer(widthPerChar, 8f, 2f);

    private static IEnumerable<LayoutFragment> EnumerateFragments(LayoutFragment fragment)
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

    private static BlockFragment FindContainerWithTexts(BlockFragment root, string firstText, string secondText)
    {
        return EnumerateFragments(root)
                   .OfType<BlockFragment>()
                   .FirstOrDefault(fragment => ContainsText(fragment, firstText) && ContainsText(fragment, secondText))
               ?? throw new ShouldAssertException("Expected to find a container with both text runs.");
    }

    private static bool ContainsText(BlockFragment fragment, string text)
    {
        return EnumerateFragments(fragment)
            .OfType<BlockFragment>()
            .SelectMany(block => block.Children.OfType<LineBoxFragment>())
            .SelectMany(line => line.Runs)
            .Any(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveDirectChildLabel(LayoutFragment fragment)
    {
        var text = EnumerateFragments(fragment)
            .OfType<LineBoxFragment>()
            .SelectMany(static line => line.Runs)
            .Select(static run => run.Text.Trim())
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

        return text?.ToLowerInvariant();
    }

    private static (float TotalHeight, float FirstBlockHeight, float SecondBlockHeight) ExtractTwoBlockMetrics(
        BlockFragment container)
    {
        var lines = EnumerateFragments(container)
            .OfType<BlockFragment>()
            .SelectMany(block => block.Children.OfType<LineBoxFragment>())
            .ToList();
        lines.Count.ShouldBeGreaterThanOrEqualTo(2);

        var firstLine = lines.First(line =>
            line.Runs.Any(run => run.Text.Contains("First", StringComparison.OrdinalIgnoreCase)));
        var secondLine = lines.First(line =>
            line.Runs.Any(run => run.Text.Contains("Second", StringComparison.OrdinalIgnoreCase)));

        return (
            container.Rect.Height,
            firstLine.Rect.Height,
            secondLine.Rect.Height);
    }

    private static IReadOnlyList<string> ExtractInlineText(BoxNode node)
    {
        return node.Children
            .OfType<InlineBox>()
            .Select(static child => child.TextContent)
            .Where(static text => !string.IsNullOrWhiteSpace(text))
            .Select(static text => text!)
            .ToList();
    }

    private static GeometrySourceIdentity CreateNodeSourceIdentity(
        int nodeId,
        string sourcePath,
        string elementIdentity) =>
        new(
            new StyleNodeId(nodeId),
            null,
            sourcePath,
            elementIdentity,
            nodeId,
            GeometryGeneratedSourceKind.None);

    private static GeometrySourceIdentity CreateContentSourceIdentity(
        int nodeId,
        int contentId,
        string sourcePath,
        string elementIdentity) =>
        new(
            new StyleNodeId(nodeId),
            new StyleContentId(contentId),
            sourcePath,
            elementIdentity,
            contentId,
            GeometryGeneratedSourceKind.AnonymousText);
}