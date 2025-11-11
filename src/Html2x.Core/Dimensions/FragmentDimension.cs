namespace Html2x.Core.Dimensions;

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