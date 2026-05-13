using Html2x.RenderModel.Text;
using Html2x.Text;

namespace Html2x.Test;

public sealed class TextMeasurementTests
{
    [Theory]
    [MemberData(nameof(InvalidNumericValues))]
    public void Constructor_InvalidNumericValue_ThrowsArgumentOutOfRange(
        string parameterName,
        float width,
        float ascent,
        float descent)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new TextMeasurement(width, ascent, descent, CreateResolvedFont()));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    public void Constructor_NullResolvedFont_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _ = new TextMeasurement(10f, 8f, 2f, null!));

        Assert.Equal("ResolvedFont", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_EmptyResolvedFontSourceId_ThrowsArgumentException(string sourceId)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _ = new TextMeasurement(10f, 8f, 2f, CreateResolvedFont(sourceId)));

        Assert.Equal("ResolvedFont", exception.ParamName);
    }

    [Fact]
    public void WithExpression_InvalidNumericValue_ThrowsArgumentOutOfRange()
    {
        var measurement = new TextMeasurement(10f, 8f, 2f, CreateResolvedFont());

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            _ = measurement with { Width = float.NaN });

        Assert.Equal("Width", exception.ParamName);
    }

    public static IEnumerable<object[]> InvalidNumericValues()
    {
        yield return ["Width", -1f, 8f, 2f];
        yield return ["Width", float.NaN, 8f, 2f];
        yield return ["Width", float.PositiveInfinity, 8f, 2f];
        yield return ["Width", float.NegativeInfinity, 8f, 2f];
        yield return ["Ascent", 10f, -1f, 2f];
        yield return ["Ascent", 10f, float.NaN, 2f];
        yield return ["Ascent", 10f, float.PositiveInfinity, 2f];
        yield return ["Ascent", 10f, float.NegativeInfinity, 2f];
        yield return ["Descent", 10f, 8f, -1f];
        yield return ["Descent", 10f, 8f, float.NaN];
        yield return ["Descent", 10f, 8f, float.PositiveInfinity];
        yield return ["Descent", 10f, 8f, float.NegativeInfinity];
    }

    private static ResolvedFont CreateResolvedFont(string sourceId = "test://font") =>
        new("Inter", FontWeight.W400, FontStyle.Normal, sourceId);
}
