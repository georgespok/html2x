using System.Drawing;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Geometry;
using Shouldly;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.Geometry;

public sealed class GeometryTranslatorTests
{
    [Fact]
    public void Translate_UsedGeometry_OffsetsBorderContentAndBaseline()
    {
        var geometry = BoxGeometryFactory.FromBorderBox(
            10f,
            20f,
            100f,
            50f,
            new Spacing(2f, 3f, 4f, 5f),
            new Spacing(1f, 1f, 1f, 1f),
            baseline: 35f,
            markerOffset: 6f);

        var translated = GeometryTranslator.Translate(geometry, 4f, -8f);

        translated.BorderBoxRect.ShouldBe(new RectangleF(14f, 12f, 100f, 50f));
        translated.ContentBoxRect.ShouldBe(new RectangleF(20f, 15f, 90f, 42f));
        translated.Baseline.ShouldBe(27f);
        translated.MarkerOffset.ShouldBe(6f);
        geometry.BorderBoxRect.ShouldBe(new RectangleF(10f, 20f, 100f, 50f));
    }

    [Fact]
    public void Translate_Rectangle_OffsetsOriginAndPreservesSize()
    {
        var translated = RenderGeometryTranslator.Translate(
            new RectangleF(10f, 20f, 30f, 40f),
            -2f,
            5f);

        translated.ShouldBe(new RectangleF(8f, 25f, 30f, 40f));
    }

    [Fact]
    public void Translate_TextRun_OffsetsOriginAndPreservesMetrics()
    {
        var run = new TextRun(
            "alpha",
            new FontKey("Test", FontWeight.W700, FontStyle.Italic),
            12f,
            new PointF(10f, 20f),
            30f,
            8f,
            3f,
            TextDecorations.Underline,
            ColorRgba.Black);

        var translated = RenderGeometryTranslator.Translate(run, -2f, 5f);

        translated.Origin.ShouldBe(new PointF(8f, 25f));
        translated.Text.ShouldBe(run.Text);
        translated.Font.ShouldBe(run.Font);
        translated.AdvanceWidth.ShouldBe(run.AdvanceWidth);
        translated.Decorations.ShouldBe(run.Decorations);
    }

}
