using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public interface IInlineLayoutEngine
{
    InlineLayoutResult Layout(BlockBox block, InlineLayoutRequest request);

    float MeasureHeight(DisplayNode block, float availableWidth)
    {
        if (block is not BlockBox blockBox)
        {
            throw new ArgumentException("Inline layout requires a block box.", nameof(block));
        }

        return Layout(blockBox, InlineLayoutRequest.ForMeasurement(availableWidth)).TotalHeight;
    }
}
