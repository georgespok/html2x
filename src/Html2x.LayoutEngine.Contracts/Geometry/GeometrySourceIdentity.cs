namespace Html2x.LayoutEngine.Models;

internal sealed record GeometrySourceIdentity
{
    public GeometrySourceIdentity(
        StyleNodeId? nodeId,
        StyleContentId? contentId,
        string? sourcePath,
        string? elementIdentity,
        int? sourceOrder,
        GeometryGeneratedSourceKind generatedKind)
    {
        if (sourceOrder.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(sourceOrder.Value);
        }

        NodeId = nodeId;
        ContentId = contentId;
        SourcePath = string.IsNullOrWhiteSpace(sourcePath) ? null : sourcePath;
        ElementIdentity = string.IsNullOrWhiteSpace(elementIdentity) ? null : elementIdentity;
        SourceOrder = sourceOrder;
        GeneratedKind = generatedKind;
    }

    public static GeometrySourceIdentity Unspecified { get; } = new(
        null,
        null,
        null,
        null,
        null,
        GeometryGeneratedSourceKind.None);

    public StyleNodeId? NodeId { get; }

    public StyleContentId? ContentId { get; }

    public string? SourcePath { get; }

    public string? ElementIdentity { get; }

    public int? SourceOrder { get; }

    public GeometryGeneratedSourceKind GeneratedKind { get; }

    public bool IsSpecified =>
        NodeId.GetValueOrDefault().IsSpecified ||
        ContentId.GetValueOrDefault().IsSpecified ||
        SourcePath is not null ||
        ElementIdentity is not null ||
        SourceOrder.HasValue ||
        GeneratedKind != GeometryGeneratedSourceKind.None;

    public static GeometrySourceIdentity FromStyleNode(StyleSourceIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (!identity.NodeId.IsSpecified &&
            string.IsNullOrWhiteSpace(identity.SourcePath) &&
            string.IsNullOrWhiteSpace(identity.ElementIdentity))
        {
            return Unspecified;
        }

        return new GeometrySourceIdentity(
            identity.NodeId,
            null,
            identity.SourcePath,
            identity.ElementIdentity,
            identity.SourceOrder,
            GeometryGeneratedSourceKind.None);
    }

    public static GeometrySourceIdentity FromStyleContent(
        StyleContentIdentity identity,
        string? elementIdentity,
        GeometryGeneratedSourceKind generatedKind)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var nodeId = identity.ParentId.IsSpecified
            ? identity.ParentId
            : (StyleNodeId?)null;
        var contentId = identity.ContentId.IsSpecified
            ? identity.ContentId
            : (StyleContentId?)null;
        var sourcePath = string.IsNullOrWhiteSpace(identity.SourcePath)
            ? null
            : identity.SourcePath;
        var sourceOrder = contentId.HasValue || sourcePath is not null
            ? identity.SourceOrder
            : (int?)null;

        return new GeometrySourceIdentity(
            nodeId,
            contentId,
            sourcePath,
            elementIdentity,
            sourceOrder,
            generatedKind);
    }

    public GeometrySourceIdentity AsGenerated(GeometryGeneratedSourceKind generatedKind)
    {
        if (generatedKind == GeometryGeneratedSourceKind.None)
        {
            throw new ArgumentException(
                "Generated geometry source identity requires a generated kind.",
                nameof(generatedKind));
        }

        return new GeometrySourceIdentity(
            NodeId,
            ContentId,
            AppendGeneratedSourcePath(SourcePath, generatedKind),
            ElementIdentity,
            SourceOrder,
            generatedKind);
    }

    public static GeometrySourceIdentity FirstSpecified(
        GeometrySourceIdentity first,
        GeometrySourceIdentity second)
    {
        return first.IsSpecified ? first : second;
    }

    private static string? AppendGeneratedSourcePath(
        string? sourcePath,
        GeometryGeneratedSourceKind generatedKind)
    {
        return string.IsNullOrWhiteSpace(sourcePath)
            ? null
            : $"{sourcePath}::{ResolveGeneratedSegment(generatedKind)}";
    }

    private static string ResolveGeneratedSegment(GeometryGeneratedSourceKind generatedKind)
    {
        return generatedKind switch
        {
            GeometryGeneratedSourceKind.AnonymousText => "anonymous-text",
            GeometryGeneratedSourceKind.ListMarker => "list-marker",
            GeometryGeneratedSourceKind.InlineBlockContent => "inline-block-content",
            GeometryGeneratedSourceKind.AnonymousBlock => "anonymous-block",
            GeometryGeneratedSourceKind.InlineBlockBoundary => "inline-block-boundary",
            GeometryGeneratedSourceKind.InlineSegment => "inline-segment",
            _ => "generated"
        };
    }
}
