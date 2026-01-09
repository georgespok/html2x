using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Measurements.Contexts;

public sealed record PercentageResolutionContext
{
    public PercentageResolutionContext(SizePt? parentSize, int passCount)
    {
        if (passCount < 1 || passCount > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(passCount), "Pass count must be 1 or 2.");
        }

        ParentSize = parentSize;
        PassCount = passCount;
    }

    public SizePt? ParentSize { get; }
    public int PassCount { get; }
}
