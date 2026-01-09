using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Measurements.Dimensions;

public sealed record ResolvedDimension
{
    public ResolvedDimension(
        string elementId,
        SizePt size,
        bool isPercentageWidth,
        bool isPercentageHeight,
        int passCount = 1,
        string? fallbackReason = null)
    {
        if (passCount < 1 || passCount > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(passCount), "Pass count must be 1 or 2.");
        }

        ElementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
        Size = size;
        IsPercentageWidth = isPercentageWidth;
        IsPercentageHeight = isPercentageHeight;
        PassCount = passCount;
        FallbackReason = fallbackReason;
    }

    public string ElementId { get; }
    public SizePt Size { get; }
    public bool IsPercentageWidth { get; }
    public bool IsPercentageHeight { get; }
    public int PassCount { get; }
    public string? FallbackReason { get; }
}
