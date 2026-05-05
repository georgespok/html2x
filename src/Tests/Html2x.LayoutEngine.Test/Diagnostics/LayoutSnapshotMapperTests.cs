using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Diagnostics;

public sealed class LayoutSnapshotMapperTests
{
    [Fact]
    public void From_FragmentWithVisualStyle_MapsVerificationFields()
    {
        var color = new ColorRgba(0x11, 0x22, 0x33, 0xFF);
        var background = new ColorRgba(0xEE, 0xDD, 0xCC, 0xFF);
        var margin = new Spacing(1, 2, 3, 4);
        var padding = new Spacing(5, 6, 7, 8);
        var borders = BorderEdges.Uniform(new BorderSide(2f, color, BorderLineStyle.Solid));
        var fragment = new BlockFragment
        {
            Rect = new RectPt(10, 20, 300, 40),
            Style = new VisualStyle(
                BackgroundColor: background,
                Borders: borders,
                Color: color,
                Margin: margin,
                Padding: padding,
                WidthPt: 300,
                HeightPt: 40,
                Display: "block"),
            DisplayRole = FragmentDisplayRole.Block
        };
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(24, 24, 24, 24),
            [fragment]));

        var snapshot = LayoutSnapshotMapper.From(layout);

        var mapped = snapshot.Pages[0].Fragments.ShouldHaveSingleItem();
        mapped.Kind.ShouldBe("block");
        mapped.Color.ShouldBe(color);
        mapped.BackgroundColor.ShouldBe(background);
        mapped.Margin.ShouldBe(margin);
        mapped.Padding.ShouldBe(padding);
        mapped.WidthPt.ShouldBe(300);
        mapped.HeightPt.ShouldBe(40);
        mapped.Display.ShouldBe("block");
        mapped.Borders.ShouldBe(borders);
        mapped.DisplayRole.ShouldBe(FragmentDisplayRole.Block);
    }

    [Fact]
    public void ToDiagnosticObject_BlockFragment_PreservesSnapshotFieldShape()
    {
        var color = new ColorRgba(0x11, 0x22, 0x33, 0xFF);
        var background = new ColorRgba(0xAA, 0xBB, 0xCC, 0xFF);
        var margin = new Spacing(1, 2, 3, 4);
        var padding = new Spacing(5, 6, 7, 8);
        var borders = BorderEdges.Uniform(new BorderSide(2f, color, BorderLineStyle.Solid));
        var fragment = new BlockFragment
        {
            Rect = new RectPt(10, 20, 300, 40),
            Style = new VisualStyle(
                BackgroundColor: background,
                Borders: borders,
                Color: color,
                Margin: margin,
                Padding: padding,
                WidthPt: 300,
                HeightPt: 40,
                Display: "block"),
            DisplayRole = FragmentDisplayRole.Block,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 9
        };
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(24, 25, 26, 27),
            [fragment]));

        var snapshot = LayoutSnapshotMapper.ToDiagnosticObject(layout);

        KeysShouldBe(snapshot, "pageCount", "pages");
        snapshot["pageCount"].ShouldBe(new DiagnosticNumberValue(1));

        var page = ObjectItem(ArrayField(snapshot, "pages"), 0);
        KeysShouldBe(page, "pageNumber", "pageSize", "margin", "fragments");
        page["pageNumber"].ShouldBe(new DiagnosticNumberValue(1));
        NumberField(ObjectField(page, "pageSize"), "width").ShouldBe(612);
        NumberField(ObjectField(page, "pageSize"), "height").ShouldBe(792);
        NumberField(ObjectField(page, "margin"), "top").ShouldBe(24);
        NumberField(ObjectField(page, "margin"), "right").ShouldBe(25);
        NumberField(ObjectField(page, "margin"), "bottom").ShouldBe(26);
        NumberField(ObjectField(page, "margin"), "left").ShouldBe(27);

        var block = ObjectItem(ArrayField(page, "fragments"), 0);
        KeysShouldBe(
            block,
            "sequenceId",
            "kind",
            "x",
            "y",
            "size",
            "color",
            "backgroundColor",
            "margin",
            "padding",
            "widthPt",
            "heightPt",
            "display",
            "text",
            "contentX",
            "contentY",
            "contentSize",
            "occupiedX",
            "occupiedY",
            "occupiedSize",
            "borders",
            "displayRole",
            "formattingContext",
            "markerOffset",
            "derivedColumnCount",
            "rowIndex",
            "columnIndex",
            "isHeader",
            "metadataOwner",
            "metadataConsumer",
            "children");
        block["sequenceId"].ShouldBe(new DiagnosticNumberValue(1));
        block["kind"].ShouldBe(new DiagnosticStringValue("block"));
        NumberField(block, "x").ShouldBe(10);
        NumberField(block, "y").ShouldBe(20);
        NumberField(ObjectField(block, "size"), "width").ShouldBe(300);
        NumberField(ObjectField(block, "size"), "height").ShouldBe(40);
        block["color"].ShouldBe(new DiagnosticStringValue(color.ToHex()));
        block["backgroundColor"].ShouldBe(new DiagnosticStringValue(background.ToHex()));
        NumberField(ObjectField(block, "margin"), "left").ShouldBe(4);
        NumberField(ObjectField(block, "padding"), "left").ShouldBe(8);
        block["widthPt"].ShouldBe(new DiagnosticNumberValue(300));
        block["heightPt"].ShouldBe(new DiagnosticNumberValue(40));
        block["display"].ShouldBe(new DiagnosticStringValue("block"));
        block["text"].ShouldBeNull();
        block["contentSize"].ShouldBeNull();
        block["occupiedSize"].ShouldBeNull();
        block["displayRole"].ShouldBe(new DiagnosticStringValue(nameof(FragmentDisplayRole.Block)));
        block["formattingContext"].ShouldBe(new DiagnosticStringValue(nameof(FormattingContextKind.Block)));
        block["markerOffset"].ShouldBe(new DiagnosticNumberValue(9));
        block["metadataOwner"].ShouldBe(new DiagnosticStringValue("FragmentBuilder"));
        block["metadataConsumer"].ShouldBe(new DiagnosticStringValue("LayoutSnapshotMapper"));
        ArrayField(block, "children").Count.ShouldBe(0);

        var borderTop = ObjectField(ObjectField(block, "borders"), "top");
        borderTop["width"].ShouldBe(new DiagnosticNumberValue(2));
        borderTop["color"].ShouldBe(new DiagnosticStringValue(color.ToHex()));
        borderTop["lineStyle"].ShouldBe(new DiagnosticStringValue(nameof(BorderLineStyle.Solid)));
    }

    [Fact]
    public void From_ImageFragmentWithVisualStyle_MapsCommonAndImageFields()
    {
        var color = new ColorRgba(0x44, 0x55, 0x66, 0xFF);
        var background = new ColorRgba(0x10, 0x20, 0x30, 0xFF);
        var margin = new Spacing(2, 4, 6, 8);
        var padding = new Spacing(1, 3, 5, 7);
        var borders = BorderEdges.Uniform(new BorderSide(1f, color, BorderLineStyle.Dashed));
        var fragment = new ImageFragment
        {
            Src = "image.png",
            Rect = new RectPt(20, 30, 100, 80),
            ContentRect = new RectPt(27, 31, 90, 70),
            Style = new VisualStyle(
                BackgroundColor: background,
                Borders: borders,
                Color: color,
                Margin: margin,
                Padding: padding,
                WidthPt: 100,
                HeightPt: 80,
                Display: "inline-block")
        };
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(
            new SizePt(612, 792),
            new Spacing(24, 24, 24, 24),
            [fragment]));

        var snapshot = LayoutSnapshotMapper.From(layout);

        var mapped = snapshot.Pages[0].Fragments.ShouldHaveSingleItem();
        mapped.Kind.ShouldBe("image");
        mapped.Color.ShouldBe(color);
        mapped.BackgroundColor.ShouldBe(background);
        mapped.Margin.ShouldBe(margin);
        mapped.Padding.ShouldBe(padding);
        mapped.WidthPt.ShouldBe(100);
        mapped.HeightPt.ShouldBe(80);
        mapped.Display.ShouldBe("inline-block");
        mapped.Borders.ShouldBe(borders);
        mapped.ContentX.ShouldBe(27);
        mapped.ContentY.ShouldBe(31);
        mapped.ContentSize.ShouldBe(new SizePt(90, 70));
    }

    [Fact]
    public void From_NestedFragmentsAcrossPages_AssignsDepthFirstSequenceIds()
    {
        var line = new LineBoxFragment
        {
            Rect = new RectPt(10, 10, 80, 12)
        };
        var block = new BlockFragment([line])
        {
            Rect = new RectPt(10, 10, 100, 20)
        };
        var image = new ImageFragment
        {
            Src = "sequence.png",
            Rect = new RectPt(10, 40, 20, 20),
            ContentRect = new RectPt(10, 40, 20, 20)
        };
        var rule = new RuleFragment
        {
            Rect = new RectPt(10, 10, 100, 2)
        };
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(new SizePt(612, 792), new Spacing(), [block, image]));
        layout.AddPage(new LayoutPage(new SizePt(612, 792), new Spacing(), [rule]));

        var snapshot = LayoutSnapshotMapper.From(layout);

        snapshot.Pages[0].Fragments[0].SequenceId.ShouldBe(1);
        snapshot.Pages[0].Fragments[0].Children.ShouldHaveSingleItem().SequenceId.ShouldBe(2);
        snapshot.Pages[0].Fragments[1].SequenceId.ShouldBe(3);
        snapshot.Pages[1].Fragments.ShouldHaveSingleItem().SequenceId.ShouldBe(4);
    }

    private static DiagnosticArray ArrayField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticArray>();

    private static DiagnosticObject ObjectField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticObject>();

    private static DiagnosticObject ObjectItem(DiagnosticArray value, int index) =>
        value[index].ShouldBeOfType<DiagnosticObject>();

    private static double NumberField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;

    private static void KeysShouldBe(DiagnosticObject value, params string[] expectedKeys)
    {
        var actual = value.Keys.OrderBy(static key => key, StringComparer.Ordinal).ToArray();
        var expected = expectedKeys.OrderBy(static key => key, StringComparer.Ordinal).ToArray();

        actual.ShouldBe(expected);
    }
}
