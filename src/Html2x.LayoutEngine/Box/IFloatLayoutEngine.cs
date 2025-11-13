using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public interface IFloatLayoutEngine
{
    void PlaceFloats(DisplayNode block, float x, float y, float width);
}