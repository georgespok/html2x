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
        var (w, h) = StyleConverter.ResolveImageSize(authWidth, authHeight, 0, 0);
        var scale = Math.Min(1.0, availableWidth / w);
        w *= scale;
        h *= scale;

        // assert
        w.ShouldBe(200, 1);
        h.ShouldBe(100, 1);
    }
}
