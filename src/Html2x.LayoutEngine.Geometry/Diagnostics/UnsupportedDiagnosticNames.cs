namespace Html2x.LayoutEngine.Geometry.Diagnostics;

internal static class UnsupportedDiagnosticNames
{
    public static class Events
    {
        public const string InlineBlockUnsupportedStructure = "layout/inline-block/unsupported-structure";
        public const string UnsupportedMode = "layout/unsupported-mode";
    }

    public static class StructureKinds
    {
        public const string Float = "float";
        public const string DisplayFlex = "display:flex";
        public const string PositionAbsolute = "position:absolute";
    }

    public static class Reasons
    {
        public const string InlineBlockUnsupportedStructure =
            "Unsupported structure encountered inside inline-block formatting context.";

        public const string CssFloats =
            "CSS floats are not implemented. The current fallback omits floated content from normal layout.";

        public const string CssFlex =
            "CSS flex layout is not implemented. The current fallback lays the container out as block flow.";

        public const string AbsolutePosition =
            "Absolute positioning is not implemented. The current fallback keeps the element in normal flow.";
    }
}