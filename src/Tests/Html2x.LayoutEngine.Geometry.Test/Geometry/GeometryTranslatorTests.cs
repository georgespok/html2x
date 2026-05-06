using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Geometry;

public sealed class GeometryTranslatorTests
{
    [Fact]
    public void Translate_UsedGeometry_OffsetsBorderContentAndBaseline()
    {
        var geometry = UsedGeometryRules.FromBorderBox(
            10f,
            20f,
            100f,
            50f,
            new(2f, 3f, 4f, 5f),
            new(1f, 1f, 1f, 1f),
            35f,
            6f);

        var translated = GeometryTranslator.Translate(geometry, 4f, -8f);

        translated.BorderBoxRect.ShouldBe(new(14f, 12f, 100f, 50f));
        translated.ContentBoxRect.ShouldBe(new(20f, 15f, 90f, 42f));
        translated.Baseline.ShouldBe(27f);
        translated.MarkerOffset.ShouldBe(6f);
        geometry.BorderBoxRect.ShouldBe(new(10f, 20f, 100f, 50f));
    }

    [Fact]
    public void Translate_Rectangle_OffsetsOriginAndPreservesSize()
    {
        var translated = new RectPt(10f, 20f, 30f, 40f).Translate(-2f, 5f);

        translated.ShouldBe(new(8f, 25f, 30f, 40f));
    }

    [Fact]
    public void Translate_TextRun_OffsetsOriginAndPreservesMetrics()
    {
        var run = new TextRun(
            "alpha",
            new("Test", FontWeight.W700, FontStyle.Italic),
            12f,
            new(10f, 20f),
            30f,
            8f,
            3f,
            TextDecorations.Underline,
            ColorRgba.Black);

        var translated = run with { Origin = run.Origin.Translate(-2f, 5f) };

        translated.Origin.ShouldBe(new(8f, 25f));
        translated.Text.ShouldBe(run.Text);
        translated.Font.ShouldBe(run.Font);
        translated.AdvanceWidth.ShouldBe(run.AdvanceWidth);
        translated.Decorations.ShouldBe(run.Decorations);
    }
}