using System.Drawing;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Diagnostics;
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
            Rect = new RectangleF(10, 20, 300, 40),
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
        layout.Pages.Add(new LayoutPage(
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
            Rect = new RectangleF(20, 30, 100, 80),
            ContentRect = new RectangleF(27, 31, 90, 70),
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
        layout.Pages.Add(new LayoutPage(
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
            Rect = new RectangleF(10, 10, 80, 12)
        };
        var block = new BlockFragment([line])
        {
            Rect = new RectangleF(10, 10, 100, 20)
        };
        var image = new ImageFragment
        {
            Src = "sequence.png",
            Rect = new RectangleF(10, 40, 20, 20),
            ContentRect = new RectangleF(10, 40, 20, 20)
        };
        var rule = new RuleFragment
        {
            Rect = new RectangleF(10, 10, 100, 2)
        };
        var layout = new HtmlLayout();
        layout.Pages.Add(new LayoutPage(new SizePt(612, 792), new Spacing(), [block, image]));
        layout.Pages.Add(new LayoutPage(new SizePt(612, 792), new Spacing(), [rule]));

        var snapshot = LayoutSnapshotMapper.From(layout);

        snapshot.Pages[0].Fragments[0].SequenceId.ShouldBe(1);
        snapshot.Pages[0].Fragments[0].Children.ShouldHaveSingleItem().SequenceId.ShouldBe(2);
        snapshot.Pages[0].Fragments[1].SequenceId.ShouldBe(3);
        snapshot.Pages[1].Fragments.ShouldHaveSingleItem().SequenceId.ShouldBe(4);
    }
}
