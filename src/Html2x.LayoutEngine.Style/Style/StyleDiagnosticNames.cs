namespace Html2x.LayoutEngine.Style.Style;

internal static class StyleDiagnosticNames
{
    public static class Stages
    {
        public const string Dom = "stage/dom";
        public const string Style = "stage/style";
    }

    public static class Events
    {
        public const string IgnoredDeclaration = "style/ignored-declaration";
        public const string UnsupportedDeclaration = "style/unsupported-declaration";
        public const string PartiallyAppliedDeclaration = "style/partially-applied-declaration";
        public const string AppliedDeclaration = "style/applied-declaration";
    }

    public static class Fields
    {
        public const string PropertyName = "propertyName";
        public const string RawValue = "rawValue";
        public const string NormalizedValue = "normalizedValue";
        public const string Decision = "decision";
        public const string Reason = "reason";
    }

    public static class Decisions
    {
        public const string Ignored = "Ignored";
        public const string Unsupported = "Unsupported";
        public const string PartiallyApplied = "PartiallyApplied";
        public const string Applied = "Applied";
    }
}