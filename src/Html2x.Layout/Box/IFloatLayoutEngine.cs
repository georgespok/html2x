namespace Html2x.Layout.Box;

public interface IFloatLayoutEngine
{
    void PlaceFloats(DisplayNode block, float x, float y, float width);
}