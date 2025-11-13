using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public interface IInlineLayoutEngine
{
    float MeasureHeight(DisplayNode block, float availableWidth);
}