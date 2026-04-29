using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Geometry.Published;

internal sealed record PublishedBlockIdentity
{
    public PublishedBlockIdentity(
        string nodePath,
        string? elementIdentity,
        int sourceOrder,
        GeometrySourceIdentity? sourceIdentity = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodePath);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceOrder);

        NodePath = nodePath;
        ElementIdentity = string.IsNullOrWhiteSpace(elementIdentity) ? null : elementIdentity;
        SourceOrder = sourceOrder;
        SourceIdentity = sourceIdentity ?? GeometrySourceIdentity.Unspecified;
    }

    public string NodePath { get; }

    public string? ElementIdentity { get; }

    public int SourceOrder { get; }

    public GeometrySourceIdentity SourceIdentity { get; }
}
