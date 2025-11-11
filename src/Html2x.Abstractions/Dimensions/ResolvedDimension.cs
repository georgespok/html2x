namespace Html2x.Core.Dimensions;

public sealed record ResolvedDimension
{
    public ResolvedDimension(
        string elementId,
        float widthPt,
        float heightPt,
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
        WidthPt = widthPt;
        HeightPt = heightPt;
        IsPercentageWidth = isPercentageWidth;
        IsPercentageHeight = isPercentageHeight;
        PassCount = passCount;
        FallbackReason = fallbackReason;
    }

    public string ElementId { get; }
    public float WidthPt { get; }
    public float HeightPt { get; }
    public bool IsPercentageWidth { get; }
    public bool IsPercentageHeight { get; }
    public int PassCount { get; }
    public string? FallbackReason { get; }
}