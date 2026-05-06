using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Geometry;

public sealed class PointSpaceGeometryTests
{
    [Theory]
    [InlineData(1f, 2f)]
    [InlineData(-4f, 5f)]
    [InlineData(0f, 0f)]
    public void PointPt_ValidCoordinates_StoresValues(float x, float y)
    {
        var point = new PointPt(x, y);

        point.X.ShouldBe(x);
        point.Y.ShouldBe(y);
    }

    [Theory]
    [InlineData(float.NaN, 0f)]
    [InlineData(0f, float.NaN)]
    [InlineData(float.PositiveInfinity, 0f)]
    [InlineData(0f, float.NegativeInfinity)]
    public void PointPt_NonFiniteCoordinate_Throws(float x, float y)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new PointPt(x, y));
    }

    [Fact]
    public void PointPt_Translate_ReturnsTranslatedPoint()
    {
        var translated = new PointPt(2f, 3f).Translate(5f, -1f);

        translated.ShouldBe(new(7f, 2f));
    }

    [Fact]
    public void RectPt_ValidValues_StoresEdgesOriginAndSize()
    {
        var rect = new RectPt(10f, 20f, 30f, 40f);

        rect.Left.ShouldBe(10f);
        rect.Top.ShouldBe(20f);
        rect.Right.ShouldBe(40f);
        rect.Bottom.ShouldBe(60f);
        rect.Origin.ShouldBe(new(10f, 20f));
        rect.Size.ShouldBe(new(30f, 40f));
    }

    [Theory]
    [InlineData(float.NaN, 0f, 1f, 1f)]
    [InlineData(0f, float.NaN, 1f, 1f)]
    [InlineData(0f, 0f, float.NaN, 1f)]
    [InlineData(0f, 0f, 1f, float.NaN)]
    [InlineData(0f, 0f, -1f, 1f)]
    [InlineData(0f, 0f, 1f, -1f)]
    public void RectPt_InvalidValue_Throws(float x, float y, float width, float height)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new RectPt(x, y, width, height));
    }

    [Fact]
    public void RectPt_Translate_PreservesSize()
    {
        var translated = new RectPt(10f, 20f, 30f, 40f).Translate(-2f, 5f);

        translated.ShouldBe(new(8f, 25f, 30f, 40f));
    }

    [Fact]
    public void RectPt_WithMethods_ReplaceSingleDimension()
    {
        var rect = new RectPt(10f, 20f, 30f, 40f);

        rect.WithX(1f).ShouldBe(new(1f, 20f, 30f, 40f));
        rect.WithY(2f).ShouldBe(new(10f, 2f, 30f, 40f));
        rect.WithWidth(3f).ShouldBe(new(10f, 20f, 3f, 40f));
        rect.WithHeight(4f).ShouldBe(new(10f, 20f, 30f, 4f));
    }

    [Fact]
    public void RectPt_Inset_ClampsDimensionsAtZero()
    {
        var rect = new RectPt(10f, 20f, 8f, 6f);
        var spacing = new Spacing(2f, 5f, 6f, 7f);

        rect.Inset(spacing).ShouldBe(new(17f, 22f, 0f, 0f));
    }
}