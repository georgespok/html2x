namespace Html2x.LayoutEngine.Geometry.Diagnostics;

internal static class TableLayoutDiagnosticNames
{
    public static class Events
    {
        public const string Table = "layout/table";
        public const string UnsupportedStructure = "layout/table/unsupported-structure";
    }

    public static class Fields
    {
        public const string TablePath = "tablePath";
        public const string CellCount = "cellCount";
        public const string DerivedColumnCount = "derivedColumnCount";
        public const string RequestedWidth = "requestedWidth";
        public const string ResolvedWidth = "resolvedWidth";
        public const string Outcome = "outcome";
        public const string Rows = "rows";
        public const string Cells = "cells";
        public const string Columns = "columns";
        public const string Groups = "groups";
        public const string GroupKind = "groupKind";
    }

    public static class Outcomes
    {
        public const string Supported = "Supported";
        public const string Unsupported = "Unsupported";
    }

    public static class GroupKinds
    {
        public const string Section = "section";
        public const string Direct = "direct";
    }
}
