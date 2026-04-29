using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Box.Publishing;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Geometry;

public sealed class PublishedLayoutContractTests
{
    [Fact]
    public void PublishedBlock_WithGeometry_CarriesImmutableProjectionFacts()
    {
        var identity = new PublishedBlockIdentity("body/div[1]", "div#content", sourceOrder: 1);
        var display = new PublishedDisplayFacts(
            FragmentDisplayRole.Block,
            FormattingContextKind.Block,
            markerOffset: null);
        var style = new ComputedStyle { FontSizePt = 14f };
        var geometry = CreateGeometry();

        var block = new PublishedBlock(
            identity,
            display,
            style,
            geometry,
            inlineLayout: null,
            image: null,
            rule: null,
            table: null,
            children: []);

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
            sourceOrder: 1,
            rule: rule);

        block.Rule.ShouldBe(rule);
        block.Image.ShouldBeNull();
        block.Table.ShouldBeNull();
    }

    [Fact]
    public void PublishedBlock_WithInlineLayout_PreservesPublishedInlineFacts()
    {
        var run = CreateTextRun();
        var source = new PublishedInlineSource("body/p[1]/span[1]", "span.note", sourceOrder: 2);
        var inlineObjectContent = CreateBlock("body/p[1]/img[1]", sourceOrder: 3);
        var textItem = new PublishedInlineTextItem(
            order: 0,
            rect: new RectangleF(1f, 2f, 30f, 10f),
            runs: [run],
            sources: [source]);
        var objectItem = new PublishedInlineObjectItem(
            order: 1,
            rect: new RectangleF(35f, 2f, 12f, 10f),
            inlineObjectContent);
        var line = new PublishedInlineLine(
            lineIndex: 0,
            rect: new RectangleF(0f, 0f, 100f, 14f),
            occupiedRect: new RectangleF(1f, 2f, 46f, 10f),
            baselineY: 11f,
            lineHeight: 14f,
            textAlign: "left",
            items: [textItem, objectItem]);
        var segment = new PublishedInlineFlowSegment(
            lines: [line],
            top: 0f,
            height: 14f);
        var inlineLayout = new PublishedInlineLayout(
            segments: [segment],
            totalHeight: 14f,
            maxLineWidth: 46f);

        var block = CreateBlock(
            "body/p[1]",
            sourceOrder: 1,
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
    public void PublishedBlockFactory_CreateBlock_MapsResolvedFactsAndProvidedIdentity()
    {
        var source = new ImageBox(BoxRole.Block)
        {
            Src = "images/logo.png",
            AuthoredSizePx = new SizePx(40d, 20d),
            IntrinsicSizePx = new SizePx(80d, 40d),
            IsMissing = true,
            Style = new ComputedStyle { FontSizePt = 14f }
        };
        var identity = PublishedBlockFactory.CreateIdentity(source, sourceOrder: 7);
        var geometry = CreateGeometry();

        var block = PublishedBlockFactory.CreateBlock(
            source,
            identity,
            geometry,
            inlineLayout: null,
            children: []);

        block.Identity.ShouldBe(identity);
        block.Identity.SourceOrder.ShouldBe(7);
        block.Display.Role.ShouldBe(FragmentDisplayRole.Block);
        block.Style.ShouldBeSameAs(source.Style);
        block.Geometry.ShouldBe(geometry);
        block.Image.ShouldNotBeNull().Src.ShouldBe("images/logo.png");
        block.Rule.ShouldBeNull();
        block.Table.ShouldBeNull();
    }

    [Fact]
    public void PublishedBlockIdentity_KeepsNodePathAndSourceIdentitySeparate()
    {
        var sourceIdentity = CreateSourceIdentity(
            nodeId: 5,
            sourcePath: "body[0]/div[1]",
            elementIdentity: "div#content",
            sourceOrder: 12);

        var identity = new PublishedBlockIdentity(
            "block/block[1]",
            "div#content",
            sourceOrder: 3,
            sourceIdentity);

        identity.NodePath.ShouldBe("block/block[1]");
        identity.SourceOrder.ShouldBe(3);
        identity.ElementIdentity.ShouldBe("div#content");
        identity.SourceIdentity.ShouldBe(sourceIdentity);
        identity.SourceIdentity.SourcePath.ShouldBe("body[0]/div[1]");
        identity.SourceIdentity.SourceOrder.ShouldBe(12);
    }

    [Fact]
    public void PublishedBlockFactory_CreateIdentity_UsesBoxSourceIdentityElementIdentity()
    {
        var sourceIdentity = CreateSourceIdentity(
            nodeId: 6,
            sourcePath: "body[0]/section[0]",
            elementIdentity: "section#from-source",
            sourceOrder: 13);
        var source = new BlockBox(BoxRole.Block)
        {
            Element = StyledElementFacts.Create(
                "section",
                (HtmlCssConstants.HtmlAttributes.Id, "from-element")),
            SourceIdentity = sourceIdentity
        };

        var identity = PublishedBlockFactory.CreateIdentity(source, sourceOrder: 9);

        identity.NodePath.ShouldBe("section");
        identity.SourceOrder.ShouldBe(9);
        identity.ElementIdentity.ShouldBe("section#from-source");
        identity.SourceIdentity.ShouldBe(sourceIdentity);
    }

    [Fact]
    public void PublishedInlineSource_CarriesSourceIdentity()
    {
        var sourceIdentity = CreateSourceIdentity(
            nodeId: 7,
            sourcePath: "body[0]/p[0]/span[0]",
            elementIdentity: "span.note",
            sourceOrder: 14);
        var source = new InlineBox(BoxRole.Inline)
        {
            Element = StyledElementFacts.Create("span"),
            SourceIdentity = sourceIdentity
        };

        var inlineSource = PublishedBlockFactory.CreateInlineSource(source, sourceOrder: 4);

        inlineSource.NodePath.ShouldBe("span");
        inlineSource.SourceOrder.ShouldBe(4);
        inlineSource.ElementIdentity.ShouldBe("span.note");
        inlineSource.SourceIdentity.ShouldBe(sourceIdentity);
    }

    [Fact]
    public void PublishedCollections_WithMutableInput_CopyValuesAndHideMutableBacking()
    {
        var child = CreateBlock("body/div[1]", sourceOrder: 1);
        var childSource = new List<PublishedBlock> { child };
        var block = CreateBlock("body/div[0]", sourceOrder: 0, childSource);

        var page = new PublishedPage(PaperSizes.A4, new Spacing(10f, 20f, 30f, 40f));
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
    public void PublishedBlock_WithNullChild_ThrowsArgumentException()
    {
        var children = new List<PublishedBlock> { null! };

        var exception = Should.Throw<ArgumentException>(() => CreateBlock(
            "body/div[0]",
            sourceOrder: 0,
            children));

        exception.ParamName.ShouldBe("children");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void PublishedBlockIdentity_WithMissingNodePath_ThrowsArgumentException(string nodePath)
    {
        var exception = Should.Throw<ArgumentException>(() => new PublishedBlockIdentity(
            nodePath,
            elementIdentity: null,
            sourceOrder: 0));

        exception.ParamName.ShouldBe("nodePath");
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void PublishedDisplayFacts_WithInvalidMarkerOffset_ThrowsArgumentOutOfRangeException(float markerOffset)
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
        PublishedInlineLayout? inlineLayout = null)
    {
        return new PublishedBlock(
            new PublishedBlockIdentity(nodePath, elementIdentity: null, sourceOrder),
            new PublishedDisplayFacts(
                FragmentDisplayRole.Block,
                FormattingContextKind.Block,
                markerOffset: null),
            new ComputedStyle(),
            CreateGeometry(),
            inlineLayout: inlineLayout,
            image: null,
            rule: rule,
            table: null,
            children ?? []);
    }

    private static UsedGeometry CreateGeometry()
    {
        return BoxGeometryFactory.FromBorderBox(
            x: 1f,
            y: 2f,
            width: 30f,
            height: 40f,
            padding: new Spacing(3f, 4f, 5f, 6f),
            border: new Spacing(1f, 2f, 3f, 4f));
    }

    private static TextRun CreateTextRun()
    {
        return new TextRun(
            "alpha",
            new FontKey("Test", FontWeight.W400, FontStyle.Normal),
            12f,
            new PointF(1f, 11f),
            30f,
            8f,
            3f);
    }

    private static GeometrySourceIdentity CreateSourceIdentity(
        int nodeId,
        string sourcePath,
        string elementIdentity,
        int sourceOrder)
    {
        return new GeometrySourceIdentity(
            new StyleNodeId(nodeId),
            null,
            sourcePath,
            elementIdentity,
            sourceOrder,
            GeometryGeneratedSourceKind.None);
    }
}
