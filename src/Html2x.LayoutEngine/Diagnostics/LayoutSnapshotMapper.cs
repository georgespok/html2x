using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Fragments;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;
using static Html2x.LayoutEngine.Diagnostics.DiagnosticSnapshotValueMapper;

namespace Html2x.LayoutEngine.Diagnostics;

/// <summary>
///     Projects assembled layout output into diagnostic snapshots and validates fragment structures that cross layout
///     boundaries.
/// </summary>
internal static class LayoutSnapshotMapper
{
    internal static LayoutSnapshot From(HtmlLayout layout)
    {
        if (layout is null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        var pages = new List<LayoutPageSnapshot>(layout.Pages.Count);
        var fragmentMapper = new FragmentSnapshotMapper();

        for (var pageIndex = 0; pageIndex < layout.Pages.Count; pageIndex++)
        {
            var page = layout.Pages[pageIndex];
            var fragments = fragmentMapper.MapFragments(page.Children);
            var pageNumber = page.PageNumber > 0 ? page.PageNumber : pageIndex + 1;

            pages.Add(new()
            {
                PageNumber = pageNumber,
                PageSize = page.PageSize,
                Margin = page.Margins,
                Fragments = fragments
            });
        }

        return new()
        {
            PageCount = layout.Pages.Count,
            Pages = pages
        };
    }

    public static DiagnosticObject ToDiagnosticObject(HtmlLayout layout) =>
        MapLayoutSnapshot(From(layout));

    internal static DiagnosticObject MapLayoutSnapshot(LayoutSnapshot snapshot) =>
        DiagnosticObject.Create(
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.PageCount, snapshot.PageCount),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.Pages,
                MapArray(snapshot.Pages, MapPageSnapshot)));

    private static DiagnosticObject MapPageSnapshot(LayoutPageSnapshot page) =>
        DiagnosticObject.Create(
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.PageNumber, page.PageNumber),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.PageSize, MapSize(page.PageSize)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Margin, MapSpacing(page.Margin)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.Fragments,
                MapArray(page.Fragments, MapFragmentSnapshot)));

    private static DiagnosticObject MapFragmentSnapshot(FragmentSnapshot fragment) =>
        DiagnosticObject.Create(
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.SequenceId, fragment.SequenceId),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Kind, fragment.Kind),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.X, fragment.X),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Y, fragment.Y),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Size, MapSize(fragment.Size)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Color, MapColor(fragment.Color)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.BackgroundColor,
                MapColor(fragment.BackgroundColor)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Margin, MapSpacing(fragment.Margin)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Padding, MapSpacing(fragment.Padding)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.WidthPt, FromNullable(fragment.WidthPt)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.HeightPt, FromNullable(fragment.HeightPt)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Display, FromNullable(fragment.Display)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Text, FromNullable(fragment.Text)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.ContentX, FromNullable(fragment.ContentX)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.ContentY, FromNullable(fragment.ContentY)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.ContentSize, MapSize(fragment.ContentSize)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.OccupiedX, FromNullable(fragment.OccupiedX)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.OccupiedY, FromNullable(fragment.OccupiedY)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.OccupiedSize, MapSize(fragment.OccupiedSize)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.Borders, MapBorders(fragment.Borders)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.DisplayRole,
                FromNullable(fragment.DisplayRole)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.FormattingContext,
                FromNullable(fragment.FormattingContext)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.MarkerOffset,
                FromNullable(fragment.MarkerOffset)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.DerivedColumnCount,
                FromNullable(fragment.DerivedColumnCount)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.RowIndex, FromNullable(fragment.RowIndex)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.ColumnIndex,
                FromNullable(fragment.ColumnIndex)),
            DiagnosticObject.Field(LayoutSnapshotSchema.Fields.IsHeader, FromNullable(fragment.IsHeader)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.MetadataOwner,
                FromNullable(fragment.MetadataOwner)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.MetadataConsumer,
                FromNullable(fragment.MetadataConsumer)),
            DiagnosticObject.Field(
                LayoutSnapshotSchema.Fields.Children,
                MapArray(fragment.Children, MapFragmentSnapshot)));

    private static FragmentSnapshot MapLineBox(LineBoxFragment line, int sequenceId)
    {
        var text = string.Concat(line.Runs.Select(r => r.Text));

        return CreateSnapshot(
            line,
            sequenceId,
            LayoutSnapshotSchema.FragmentKinds.Line,
            [],
            ResolveLineColor(line),
            text,
            occupiedX: line.OccupiedRect.X,
            occupiedY: line.OccupiedRect.Y,
            occupiedSize: new SizePt(line.OccupiedRect.Width, line.OccupiedRect.Height));
    }

    private static FragmentSnapshot MapImage(ImageFragment image, int sequenceId) =>
        CreateSnapshot(
            image,
            sequenceId,
            LayoutSnapshotSchema.FragmentKinds.Image,
            [],
            contentX: image.ContentRect.X,
            contentY: image.ContentRect.Y,
            contentSize: image.ContentSize);

    private static FragmentSnapshot MapRule(RuleFragment rule, int sequenceId) =>
        CreateSnapshot(rule, sequenceId, LayoutSnapshotSchema.FragmentKinds.Rule, []);

    private static FragmentSnapshot MapUnknown(LayoutFragment fragment, int sequenceId) =>
        CreateSnapshot(fragment, sequenceId, fragment.GetType().Name.ToLowerInvariant(), []);

    private static FragmentSnapshot CreateSnapshot(
        LayoutFragment fragment,
        int sequenceId,
        string kind,
        IReadOnlyList<FragmentSnapshot> children,
        ColorRgba? color = null,
        string? text = null,
        float? contentX = null,
        float? contentY = null,
        SizePt? contentSize = null,
        float? occupiedX = null,
        float? occupiedY = null,
        SizePt? occupiedSize = null,
        FragmentDisplayRole? displayRole = null,
        FormattingContextKind? formattingContext = null,
        float? markerOffset = null,
        int? derivedColumnCount = null,
        int? rowIndex = null,
        int? columnIndex = null,
        bool? isHeader = null,
        string? metadataOwner = null,
        string? metadataConsumer = null)
    {
        var style = fragment.Style;
        return new()
        {
            SequenceId = sequenceId,
            Kind = kind,
            X = fragment.Rect.X,
            Y = fragment.Rect.Y,
            Size = fragment.Size,
            Color = color ?? style.Color,
            BackgroundColor = style.BackgroundColor,
            Margin = style.Margin,
            Padding = style.Padding,
            WidthPt = style.WidthPt,
            HeightPt = style.HeightPt,
            Display = style.Display,
            Text = text,
            ContentX = contentX,
            ContentY = contentY,
            ContentSize = contentSize,
            OccupiedX = occupiedX,
            OccupiedY = occupiedY,
            OccupiedSize = occupiedSize,
            Borders = style.Borders,
            DisplayRole = displayRole,
            FormattingContext = formattingContext,
            MarkerOffset = markerOffset,
            DerivedColumnCount = derivedColumnCount,
            RowIndex = rowIndex,
            ColumnIndex = columnIndex,
            IsHeader = isHeader,
            MetadataOwner = metadataOwner,
            MetadataConsumer = metadataConsumer,
            Children = children
        };
    }

    private static FragmentSnapshot CreateBlockLikeSnapshot(
        BlockFragment fragment,
        int sequenceId,
        string kind,
        IReadOnlyList<FragmentSnapshot> children,
        int? derivedColumnCount = null,
        int? rowIndex = null,
        int? columnIndex = null,
        bool? isHeader = null) =>
        CreateSnapshot(
            fragment,
            sequenceId,
            kind,
            children,
            displayRole: fragment.DisplayRole,
            formattingContext: fragment.FormattingContext,
            markerOffset: fragment.MarkerOffset,
            derivedColumnCount: derivedColumnCount,
            rowIndex: rowIndex,
            columnIndex: columnIndex,
            isHeader: isHeader,
            metadataOwner: PublishedLayoutToFragmentProjector.MetadataOwnerName,
            metadataConsumer: nameof(LayoutSnapshotMapper));

    private static ColorRgba? ResolveLineColor(LineBoxFragment line)
        => line.Style?.Color ?? line.Runs.FirstOrDefault(static run => run.Color is not null)?.Color;

    private sealed class FragmentSnapshotMapper
    {
        private int _sequenceId;

        public IReadOnlyList<FragmentSnapshot> MapFragments(IEnumerable<LayoutFragment> fragments)
        {
            var snapshots = new List<FragmentSnapshot>();
            foreach (var fragment in fragments)
            {
                snapshots.Add(MapFragment(fragment));
            }

            return snapshots;
        }

        private FragmentSnapshot MapFragment(LayoutFragment fragment)
        {
            return fragment switch
            {
                TableFragment table => MapTable(table),
                TableRowFragment row => MapTableRow(row),
                TableCellFragment cell => MapTableCell(cell),
                LineBoxFragment line => MapLineBox(line, NextSequenceId()),
                BlockFragment block => MapBlock(block),
                ImageFragment image => MapImage(image, NextSequenceId()),
                RuleFragment rule => MapRule(rule, NextSequenceId()),
                _ => MapUnknown(fragment, NextSequenceId())
            };
        }

        private FragmentSnapshot MapTable(TableFragment table)
        {
            var fragmentSequenceId = NextSequenceId();
            var children = MapFragments(table.Rows);

            return CreateBlockLikeSnapshot(
                table,
                fragmentSequenceId,
                LayoutSnapshotSchema.FragmentKinds.Table,
                children,
                table.DerivedColumnCount);
        }

        private FragmentSnapshot MapTableRow(TableRowFragment row)
        {
            var fragmentSequenceId = NextSequenceId();
            var children = MapFragments(row.Cells);

            return CreateBlockLikeSnapshot(
                row,
                fragmentSequenceId,
                LayoutSnapshotSchema.FragmentKinds.TableRow,
                children,
                rowIndex: row.RowIndex);
        }

        private FragmentSnapshot MapTableCell(TableCellFragment cell)
        {
            var fragmentSequenceId = NextSequenceId();
            var children = MapFragments(cell.Children);

            return CreateBlockLikeSnapshot(
                cell,
                fragmentSequenceId,
                LayoutSnapshotSchema.FragmentKinds.TableCell,
                children,
                columnIndex: cell.ColumnIndex,
                isHeader: cell.IsHeader);
        }

        private FragmentSnapshot MapBlock(BlockFragment block)
        {
            var fragmentSequenceId = NextSequenceId();
            var children = MapFragments(block.Children);

            return CreateBlockLikeSnapshot(
                block,
                fragmentSequenceId,
                LayoutSnapshotSchema.FragmentKinds.Block,
                children);
        }

        private int NextSequenceId()
        {
            _sequenceId++;
            return _sequenceId;
        }
    }
}