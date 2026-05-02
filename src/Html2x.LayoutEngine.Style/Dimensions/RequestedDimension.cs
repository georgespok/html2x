namespace Html2x.LayoutEngine.Style.Dimensions;

internal sealed record RequestedDimension(
    string ElementId,
    float? WidthValue,
    float? HeightValue,
    DimensionUnitEnum UnitEnum,
    string? SourceDeclaration = null)
{
    public string ElementId { get; } = ElementId ?? throw new ArgumentNullException(nameof(ElementId));
}
