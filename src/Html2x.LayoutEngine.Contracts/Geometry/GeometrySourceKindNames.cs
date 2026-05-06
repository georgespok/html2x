namespace Html2x.LayoutEngine.Contracts.Geometry;

internal static class GeometrySourceKindNames
{
    public const string AnonymousText = "anonymous-text";
    public const string ListMarker = "list-marker";
    public const string InlineBlockContent = "inline-block-content";
    public const string AnonymousBlock = "anonymous-block";
    public const string InlineBlockBoundary = "inline-block-boundary";
    public const string InlineSegment = "inline-segment";
    public const string Generated = "generated";

    public static string Resolve(GeometryGeneratedSourceKind generatedKind)
    {
        return generatedKind switch
        {
            GeometryGeneratedSourceKind.AnonymousText => AnonymousText,
            GeometryGeneratedSourceKind.ListMarker => ListMarker,
            GeometryGeneratedSourceKind.InlineBlockContent => InlineBlockContent,
            GeometryGeneratedSourceKind.AnonymousBlock => AnonymousBlock,
            GeometryGeneratedSourceKind.InlineBlockBoundary => InlineBlockBoundary,
            GeometryGeneratedSourceKind.InlineSegment => InlineSegment,
            _ => Generated
        };
    }
}