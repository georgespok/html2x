using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Measurements.Dimensions;

public sealed record FragmentDimension(
    string ElementId,
    float WidthPt,
    float HeightPt,
    float? BorderThicknessPt,
    DimensionOverflowBehaviorEnum OverflowBehaviorEnum,
    DimensionDiagnostics Diagnostics)
{
    public string ElementId { get; } = ElementId ?? throw new ArgumentNullException(nameof(ElementId));
    public DimensionDiagnostics Diagnostics { get; } = Diagnostics ?? throw new ArgumentNullException(nameof(Diagnostics));
}
