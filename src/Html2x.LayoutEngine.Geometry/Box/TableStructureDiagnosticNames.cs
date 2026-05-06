namespace Html2x.LayoutEngine.Geometry.Box;

internal static class TableStructureDiagnosticNames
{
    public static class StructureKinds
    {
        public const string UnsupportedTableStructure = "unsupported-table-structure";
        public const string UnsupportedTableChild = "unsupported-table-child";
        public const string MalformedSectionNesting = "malformed-section-nesting";
        public const string UnsupportedRowChild = "unsupported-row-child";
    }

    public static class Reasons
    {
        public const string UnsupportedTableStructure = "Unsupported table structure.";

        public const string MixedDirectRowsAndSections =
            "Tables cannot mix direct rows with explicit table sections.";

        public const string NestedTableSections =
            "Table sections cannot contain nested table sections.";

        public const string UnsupportedColspan = "Table cell colspan is not supported.";
        public const string UnsupportedRowspan = "Table cell rowspan is not supported.";
    }
}