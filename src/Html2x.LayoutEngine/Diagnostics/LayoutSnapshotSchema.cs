namespace Html2x.LayoutEngine.Diagnostics;

internal static class LayoutSnapshotSchema
{
    public static class Fields
    {
        public const string PageCount = "pageCount";
        public const string Pages = "pages";
        public const string PageNumber = "pageNumber";
        public const string PageSize = "pageSize";
        public const string Margin = "margin";
        public const string Fragments = "fragments";
        public const string SequenceId = "sequenceId";
        public const string Kind = "kind";
        public const string X = "x";
        public const string Y = "y";
        public const string Size = "size";
        public const string Color = "color";
        public const string BackgroundColor = "backgroundColor";
        public const string Padding = "padding";
        public const string WidthPt = "widthPt";
        public const string HeightPt = "heightPt";
        public const string Display = "display";
        public const string Text = "text";
        public const string ContentX = "contentX";
        public const string ContentY = "contentY";
        public const string ContentSize = "contentSize";
        public const string OccupiedX = "occupiedX";
        public const string OccupiedY = "occupiedY";
        public const string OccupiedSize = "occupiedSize";
        public const string Borders = "borders";
        public const string DisplayRole = "displayRole";
        public const string FormattingContext = "formattingContext";
        public const string MarkerOffset = "markerOffset";
        public const string DerivedColumnCount = "derivedColumnCount";
        public const string RowIndex = "rowIndex";
        public const string ColumnIndex = "columnIndex";
        public const string IsHeader = "isHeader";
        public const string MetadataOwner = "metadataOwner";
        public const string MetadataConsumer = "metadataConsumer";
        public const string Children = "children";
        public const string Width = "width";
        public const string Height = "height";
        public const string Top = "top";
        public const string Right = "right";
        public const string Bottom = "bottom";
        public const string Left = "left";
        public const string LineStyle = "lineStyle";
    }

    public static class FragmentKinds
    {
        public const string Line = "line";
        public const string Image = "image";
        public const string Rule = "rule";
        public const string Table = "table";
        public const string TableRow = "table-row";
        public const string TableCell = "table-cell";
        public const string Block = "block";
    }
}
