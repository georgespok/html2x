using Html2x.LayoutEngine.Fragment;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Builders;

public class BoxTreeBuilderImageFitTests
{
    [Fact]
    public void ResolveImageSize_CappedByAvailableWidth_PreservesAspect()
    {
        // arrange
        var availableWidth = 200d;
        var (authWidth, authHeight) = (300d, 150d);

        // act
        var size = StyleConverter.ResolveImageSize(
            new Abstractions.Measurements.Units.SizePx(authWidth, authHeight),
            new Abstractions.Measurements.Units.SizePx(0, 0));
        var scale = Math.Min(1d, availableWidth / size.WidthOrZero);
        var scaled = size.Scale(scale);

        // assert
        scaled.WidthOrZero.ShouldBe(200d, 1);
        scaled.HeightOrZero.ShouldBe(100d, 1);
    }
}
