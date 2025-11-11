using Html2x.Abstractions.Layout.Styles;
using Html2x.Renderers.Pdf.Mapping;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test;

public class BorderPainterTests
{
    [Fact]
    public void GetUniformBorder_ShouldReturnBorder_WhenUniformSolid()
    {
        // Arrange
        var border = new BorderSide(2f, new ColorRgba(10, 20, 30, 255), BorderLineStyle.Solid);
        var edges = BorderEdges.Uniform(border);

        // Act
        var result = BorderPainter.GetUniformBorder(edges);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(border);
    }

    [Fact]
    public void GetUniformBorder_ShouldReturnNull_WhenEdgesDiffer()
    {
        // Arrange
        var edges = new BorderEdges
        {
            Top = new BorderSide(1f, new ColorRgba(0, 0, 0, 255), BorderLineStyle.Solid),
            Bottom = new BorderSide(2f, new ColorRgba(0, 0, 0, 255), BorderLineStyle.Solid)
        };

        // Act
        var result = BorderPainter.GetUniformBorder(edges);

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(BorderLineStyle.Solid)]
    [InlineData(BorderLineStyle.Dashed)]
    [InlineData(BorderLineStyle.Dotted)]
    public void GetUniformBorder_ShouldReturnBorder_ForSupportedLineStyles(BorderLineStyle style)
    {
        // Arrange
        var border = new BorderSide(1.5f, new ColorRgba(5, 5, 5, 255), style);
        var edges = BorderEdges.Uniform(border);

        // Act
        var result = BorderPainter.GetUniformBorder(edges);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(border);
    }

    [Fact]
    public void GetUniformBorder_ShouldReturnNull_WhenBorderDisabled()
    {
        // Arrange & Act
        var result = BorderPainter.GetUniformBorder(BorderEdges.None);

        // Assert
        result.ShouldBeNull();
    }
}

