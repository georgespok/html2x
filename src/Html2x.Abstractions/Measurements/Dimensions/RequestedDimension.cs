using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Measurements.Dimensions;

public sealed record RequestedDimension(
    string ElementId,
    float? WidthValue,
    float? HeightValue,
    DimensionUnitEnum UnitEnum,
    string? SourceDeclaration = null)
{
    public string ElementId { get; } = ElementId ?? throw new ArgumentNullException(nameof(ElementId));
}
