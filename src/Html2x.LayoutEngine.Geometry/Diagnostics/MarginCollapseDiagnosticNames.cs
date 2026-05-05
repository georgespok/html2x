namespace Html2x.LayoutEngine.Geometry.Diagnostics;

internal static class MarginCollapseDiagnosticNames
{
    public const string Event = "layout/margin-collapse";

    public static class Fields
    {
        public const string PreviousBottomMargin = "previousBottomMargin";
        public const string NextTopMargin = "nextTopMargin";
        public const string CollapsedTopMargin = "collapsedTopMargin";
        public const string Owner = "owner";
        public const string Consumer = "consumer";
    }
}
