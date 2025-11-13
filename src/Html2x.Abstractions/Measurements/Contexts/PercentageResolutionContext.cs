namespace Html2x.Abstractions.Measurements.Contexts;

public sealed record PercentageResolutionContext
{
    public PercentageResolutionContext(float? parentWidthPt, float? parentHeightPt, int passCount)
    {
        if (passCount < 1 || passCount > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(passCount), "Pass count must be 1 or 2.");
        }

        ParentWidthPt = parentWidthPt;
        ParentHeightPt = parentHeightPt;
        PassCount = passCount;
    }

    public float? ParentWidthPt { get; }
    public float? ParentHeightPt { get; }
    public int PassCount { get; }
}
