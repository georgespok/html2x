namespace Html2x.Core.Dimensions;

public sealed record DimensionDiagnostics(
    string ElementId,
    RequestedDimension? Requested,
    ResolvedDimension Resolved,
    string? Notes = null)
{
    public string ElementId { get; } = ElementId ?? throw new ArgumentNullException(nameof(ElementId));
    public ResolvedDimension Resolved { get; } = Resolved ?? throw new ArgumentNullException(nameof(Resolved));
}