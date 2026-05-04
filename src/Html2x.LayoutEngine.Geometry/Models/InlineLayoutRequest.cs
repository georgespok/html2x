using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Geometry.Models;


internal readonly record struct InlineLayoutRequest(
    float ContentLeft,
    float ContentTop,
    float AvailableWidth,
    bool IncludeSyntheticListMarker = true)
{
    public static InlineLayoutRequest ForMeasurement(float availableWidth, bool includeSyntheticListMarker = true)
    {
        return new InlineLayoutRequest(0f, 0f, availableWidth, includeSyntheticListMarker);
    }
}
