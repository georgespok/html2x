namespace Html2x.LayoutEngine.Geometry.Diagnostics;

internal static class GeometryDiagnosticNames
{
    public static class Stages
    {
        public const string BoxTree = "stage/box-tree";
    }

    public static class Fields
    {
        public const string NodePath = "nodePath";
        public const string StructureKind = "structureKind";
        public const string Reason = "reason";
        public const string FormattingContext = "formattingContext";
        public const string Width = "width";
        public const string Height = "height";
        public const string RowIndex = "rowIndex";
        public const string ColumnIndex = "columnIndex";
        public const string IsHeader = "isHeader";
        public const string RowCount = "rowCount";
    }

    public static class Consumers
    {
        public const string BlockBoxLayout = "BlockLayoutEngine";
        public const string InlineLayoutEngine = "InlineLayoutEngine";
    }

    public static class Owners
    {
        public const string BlockFormattingContext = "BlockFormattingContext";
    }
}