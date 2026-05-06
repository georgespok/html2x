using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Box.Publishing;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Geometry;

public sealed class PublishedLayoutContractTests
{
    [Fact]
    public void PublishedBlock_WithGeometry_CarriesImmutableProjectionFacts()
    {
        var identity = new PublishedBlockIdentity("body/div[1]", "div#content", 1);
        var display = new PublishedDisplayFacts(
            FragmentDisplayRole.Block,
            FormattingContextKind.Block,
            null);
        var style = new VisualStyle { Color = ColorRgba.Black };
        var geometry = CreateGeometry();

        var block = new PublishedBlock(
            identity,
            display,
            style,
            geometry,
            null,
            null,
            null,
            null,
            []);

        block.Identity.ShouldBe(identity);
        block.Display.ShouldBe(display);
        block.Style.ShouldBe(style);
        block.Geometry.ShouldBe(geometry);
        block.InlineLayout.ShouldBeNull();
        block.Image.ShouldBeNull();
        block.Rule.ShouldBeNull();
        block.Table.ShouldBeNull();
        block.Children.ShouldBeEmpty();
    }

    [Fact]
    public void PublishedBlock_WithRuleFacts_PreservesRuleIdentity()
    {
        var rule = new PublishedRuleFacts();

        var block = CreateBlock(
            "body/hr[1]",
            1,
            rule: rule);

        block.Rule.ShouldBe(rule);
        block.Image.ShouldBeNull();
        block.Table.ShouldBeNull();
    }

    [Fact]
    public void PublishedBlock_WithInlineLayout_PreservesPublishedInlineFacts()
    {
        var run = CreateTextRun();
        var source = new PublishedInlineSource("body/p[1]/span[1]", "span.note", 2);
        var inlineObjectContent = CreateBlock("body/p[1]/img[1]", 3);
        var textItem = new PublishedInlineTextItem(
            0,
            new(1f, 2f, 30f, 10f),
            [run],
            [source]);
        var objectItem = new PublishedInlineObjectItem(
            1,
            new(35f, 2f, 12f, 10f),
            inlineObjectContent);
        var line = new PublishedInlineLine(
            0,
            new(0f, 0f, 100f, 14f),
            new(1f, 2f, 46f, 10f),
            11f,
            14f,
            "left",
            [textItem, objectItem]);
        var segment = new PublishedInlineFlowSegment(
            [line],
            0f,
            14f);
        var inlineLayout = new PublishedInlineLayout(
            [segment],
            14f,
            46f);

        var block = CreateBlock(
            "body/p[1]",
            1,
            inlineLayout: inlineLayout);

        block.InlineLayout.ShouldBe(inlineLayout);
        block.InlineLayout!.Segments.ShouldHaveSingleItem().ShouldBe(segment);
        textItem.Runs.ShouldHaveSingleItem().ShouldBe(run);
        textItem.Sources.ShouldHaveSingleItem().ShouldBe(source);
        objectItem.Content.ShouldBe(inlineObjectContent);
        source.NodePath.ShouldBe("body/p[1]/span[1]");
        (inlineLayout.Segments is PublishedInlineFlowSegment[]).ShouldBeFalse();
        (segment.Lines is PublishedInlineLine[]).ShouldBeFalse();
        (line.Items is PublishedInlineItem[]).ShouldBeFalse();
        (textItem.Runs is TextRun[]).ShouldBeFalse();
        (textItem.Sources is PublishedInlineSource[]).ShouldBeFalse();
    }

    [Fact]
    public void PublishedBlockFacts_CreateBlock_MapsResolvedFactsAndProvidedIdentity()
    {
        var source = new ImageBox(BoxRole.Block)
        {
            Src = "images/logo.png",
            AuthoredSizePx = new(40d, 20d),
            IntrinsicSizePx = new(80d, 40d),
            Status = ImageLoadStatus.Missing,
            Style = new() { FontSizePt = 14f, WidthPt = 42f }
        };
        var identity = PublishedBlockFacts.CreateIdentity(source, 7);
        var geometry = CreateGeometry();

        var block = PublishedBlockFacts.CreateBlock(
            source,
            identity,
            geometry,
            null,
            []);

        block.Identity.ShouldBe(identity);
        block.Identity.SourceOrder.ShouldBe(7);
        block.Display.Role.ShouldBe(FragmentDisplayRole.Block);
        block.Style.WidthPt.ShouldBe(source.Style.WidthPt);
        block.Geometry.ShouldBe(geometry);
        block.Image.ShouldNotBeNull().Src.ShouldBe("images/logo.png");
        block.Rule.ShouldBeNull();
        block.Table.ShouldBeNull();
    }

    [Fact]
    public void PublishedBlockIdentity_KeepsNodePathAndSourceIdentitySeparate()
    {
        var sourceIdentity = CreateSourceIdentity(
            5,
            "body[0]/div[1]",
            "div#content",
            12);

        var identity = new PublishedBlockIdentity(
            "block/block[1]",
            "div#content",
            3,
            sourceIdentity);

        identity.NodePath.ShouldBe("block/block[1]");
        identity.SourceOrder.ShouldBe(3);
        identity.ElementIdentity.ShouldBe("div#content");
        identity.SourceIdentity.ShouldBe(sourceIdentity);
        identity.SourceIdentity.SourcePath.ShouldBe("body[0]/div[1]");
        identity.SourceIdentity.SourceOrder.ShouldBe(12);
    }

    [Fact]
    public void PublishedBlockFacts_CreateIdentity_UsesBoxElementIdentity()
    {
        var sourceIdentity = CreateSourceIdentity(
            6,
            "body[0]/section[0]",
            "section#from-source",
            13);
        var source = new BlockBox(BoxRole.Block)
        {
            Element = StyledElementFacts.Create(
                "section",
                (HtmlCssConstants.HtmlAttributes.Id, "from-element")),
            SourceIdentity = sourceIdentity
        };

        var identity = PublishedBlockFacts.CreateIdentity(source, 9);

        identity.NodePath.ShouldBe("section");
        identity.SourceOrder.ShouldBe(9);
        identity.ElementIdentity.ShouldBe("section#from-source");
        identity.SourceIdentity.ShouldBe(sourceIdentity);
    }

    [Fact]
    public void PublishedInlineSource_CarriesSourceIdentity()
    {
        var sourceIdentity = CreateSourceIdentity(
            7,
            "body[0]/p[0]/span[0]",
            "span.note",
            14);
        var source = new InlineBox(BoxRole.Inline)
        {
            Element = StyledElementFacts.Create("span"),
            SourceIdentity = sourceIdentity
        };

        var inlineSource = PublishedBlockFacts.CreateInlineSource(source, 4);

        inlineSource.NodePath.ShouldBe("span");
        inlineSource.SourceOrder.ShouldBe(4);
        inlineSource.ElementIdentity.ShouldBe("span.note");
        inlineSource.SourceIdentity.ShouldBe(sourceIdentity);
    }

    [Fact]
    public void PublishedCollections_WithMutableInput_CopyValuesAndHideMutableBacking()
    {
        var child = CreateBlock("body/div[1]", 1);
        var childSource = new List<PublishedBlock> { child };
        var block = CreateBlock("body/div[0]", 0, childSource);

        var page = new PublishedPage(PaperSizes.A4, new(10f, 20f, 30f, 40f));
        var blockSource = new List<PublishedBlock> { block };
        var tree = new PublishedLayoutTree(page, blockSource);

        childSource.Clear();
        blockSource.Clear();

        block.Children.ShouldHaveSingleItem().ShouldBe(child);
        tree.Blocks.ShouldHaveSingleItem().ShouldBe(block);
        (block.Children is PublishedBlock[]).ShouldBeFalse();
        (tree.Blocks is PublishedBlock[]).ShouldBeFalse();
    }

    [Fact]
    public async Task Build_InlineText_PublishedRunsIncludeResolvedFont()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <p style='margin: 0;'>alpha</p>
              </body>
            </html>
            """);

        var run = EnumeratePublishedTextRuns(result.PublishedLayout.Blocks)
            .Single(static candidate => candidate.Text == "alpha");

        run.ResolvedFont.ShouldNotBeNull();
        run.ResolvedFont.SourceId.ShouldStartWith("fallback://");
    }

    [Fact]
    public void PublishedBlock_WithNullChild_ThrowsArgumentException()
    {
        var children = new List<PublishedBlock> { null! };

        var exception = Should.Throw<ArgumentException>(() => CreateBlock(
            "body/div[0]",
            0,
            children));

        exception.ParamName.ShouldBe("block");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void PublishedBlockIdentity_WithMissingNodePath_ThrowsArgumentException(string nodePath)
    {
        var exception = Should.Throw<ArgumentException>(() => new PublishedBlockIdentity(
            nodePath,
            null,
            0));

        exception.ParamName.ShouldBe("nodePath");
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void PublishedDisplayFacts_InvalidMarkerOffset_ThrowsOutOfRange(float markerOffset)
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => new PublishedDisplayFacts(
            FragmentDisplayRole.Block,
            FormattingContextKind.Block,
            markerOffset));

        exception.ParamName.ShouldBe("markerOffset");
    }

    private static PublishedBlock CreateBlock(
        string nodePath,
        int sourceOrder,
        IReadOnlyList<PublishedBlock>? children = null,
        PublishedRuleFacts? rule = null,
        PublishedInlineLayout? inlineLayout = null) =>
        new(
            new(nodePath, null, sourceOrder),
            new(
                FragmentDisplayRole.Block,
                FormattingContextKind.Block,
                null),
            new(),
            CreateGeometry(),
            inlineLayout,
            null,
            rule,
            null,
            children ?? []);

    private static UsedGeometry CreateGeometry() =>
        UsedGeometryRules.FromBorderBox(
            1f,
            2f,
            30f,
            40f,
            new(3f, 4f, 5f, 6f),
            new(1f, 2f, 3f, 4f));

    private static TextRun CreateTextRun() =>
        new(
            "alpha",
            new("Test", FontWeight.W400, FontStyle.Normal),
            12f,
            new(1f, 11f),
            30f,
            8f,
            3f);

    private static IEnumerable<TextRun> EnumeratePublishedTextRuns(IEnumerable<PublishedBlock> blocks)
    {
        foreach (var block in blocks)
        {
            if (block.InlineLayout is not null)
            {
                foreach (var segment in block.InlineLayout.Segments)
                {
                    foreach (var line in segment.Lines)
                    {
                        foreach (var item in line.Items.OfType<PublishedInlineTextItem>())
                        {
                            foreach (var run in item.Runs)
                            {
                                yield return run;
                            }
                        }
                    }
                }
            }

            foreach (var childRun in EnumeratePublishedTextRuns(block.Children))
            {
                yield return childRun;
            }
        }
    }

    private static GeometrySourceIdentity CreateSourceIdentity(
        int nodeId,
        string sourcePath,
        string elementIdentity,
        int sourceOrder) =>
        new(
            new StyleNodeId(nodeId),
            null,
            sourcePath,
            elementIdentity,
            sourceOrder,
            GeometryGeneratedSourceKind.None);
}