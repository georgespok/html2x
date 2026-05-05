namespace Html2x.LayoutEngine.Diagnostics;

internal static class GeometrySnapshotSchema
{
    public static class Fields
    {
        public const string Boxes = "boxes";
        public const string Pagination = "pagination";
        public const string ContentTop = "contentTop";
        public const string ContentBottom = "contentBottom";
        public const string Placements = "placements";
        public const string FragmentId = "fragmentId";
        public const string OrderIndex = "orderIndex";
        public const string IsOversized = "isOversized";
        public const string DecisionKind = "decisionKind";
        public const string Path = "path";
        public const string TagName = "tagName";
        public const string SourceNodeId = "sourceNodeId";
        public const string SourceContentId = "sourceContentId";
        public const string SourcePath = "sourcePath";
        public const string SourceOrder = "sourceOrder";
        public const string SourceElementIdentity = "sourceElementIdentity";
        public const string GeneratedSourceKind = "generatedSourceKind";
        public const string Baseline = "baseline";
        public const string AllowsOverflow = "allowsOverflow";
        public const string IsAnonymous = "isAnonymous";
        public const string IsInlineBlockContext = "isInlineBlockContext";
    }

    public static class Metadata
    {
        public const string PaginationConsumer = "Pagination";
        public const string BoxGeometryOwner = "BlockLayoutEngine";
    }
}
