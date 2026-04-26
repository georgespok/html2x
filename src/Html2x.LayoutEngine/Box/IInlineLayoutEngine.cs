using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Lays out or measures inline content inside a block formatting context.
/// </summary>
public interface IInlineLayoutEngine
{
    // Full layout owns BlockBox.InlineLayout for the layout geometry stage.
    InlineLayoutResult Layout(BlockBox block, InlineLayoutRequest request);

    // Measurement is read-only from the caller's point of view and must restore existing inline layout state.
    InlineLayoutResult Measure(BlockBox block, InlineLayoutRequest request)
    {
        throw new NotSupportedException(
            $"{nameof(Measure)} must be implemented as a read-only measurement path. " +
            $"{nameof(Layout)} is allowed to mutate layout geometry and cannot be used as the default implementation.");
    }

    float MeasureHeight(DisplayNode block, float availableWidth)
    {
        if (block is not BlockBox blockBox)
        {
            throw new ArgumentException("Inline layout requires a block box.", nameof(block));
        }

        return Measure(blockBox, InlineLayoutRequest.ForMeasurement(availableWidth)).TotalHeight;
    }
}
