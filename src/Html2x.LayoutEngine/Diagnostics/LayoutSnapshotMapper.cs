using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Fragments;
using LayoutFragment = Html2x.RenderModel.Fragment;

namespace Html2x.LayoutEngine.Diagnostics;

/// <summary>
/// Projects assembled layout output into diagnostic snapshots and validates fragment structures that cross layout boundaries.
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

            pages.Add(new LayoutPageSnapshot
            {
                PageNumber = pageNumber,
                PageSize = page.PageSize,
                Margin = page.Margins,
                Fragments = fragments
            });
        }

        return new LayoutSnapshot
        {
            PageCount = layout.Pages.Count,
            Pages = pages
        };
    }

    public static DiagnosticObject ToDiagnosticObject(HtmlLayout layout) =>
        MapLayoutSnapshot(From(layout));

    internal static DiagnosticObject MapLayoutSnapshot(LayoutSnapshot snapshot)
    {
        return DiagnosticObject.Create(
            DiagnosticObject.Field("pageCount", snapshot.PageCount),
            DiagnosticObject.Field(
                "pages",
                new DiagnosticArray(snapshot.Pages.Select(MapPageSnapshot))));
    }

    private static DiagnosticObject MapPageSnapshot(LayoutPageSnapshot page)
    {
        return DiagnosticObject.Create(
            DiagnosticObject.Field("pageNumber", page.PageNumber),
            DiagnosticObject.Field("pageSize", MapSize(page.PageSize)),
            DiagnosticObject.Field("margin", MapSpacing(page.Margin)),
            DiagnosticObject.Field("fragments", new DiagnosticArray(page.Fragments.Select(MapFragmentSnapshot))));
    }

    private static DiagnosticObject MapFragmentSnapshot(FragmentSnapshot fragment)
    {
        return DiagnosticObject.Create(
            DiagnosticObject.Field("sequenceId", fragment.SequenceId),
            DiagnosticObject.Field("kind", fragment.Kind),
            DiagnosticObject.Field("x", fragment.X),
            DiagnosticObject.Field("y", fragment.Y),
            DiagnosticObject.Field("size", MapSize(fragment.Size)),
            DiagnosticObject.Field("color", MapColor(fragment.Color)),
            DiagnosticObject.Field("backgroundColor", MapColor(fragment.BackgroundColor)),
            DiagnosticObject.Field("margin", MapSpacing(fragment.Margin)),
            DiagnosticObject.Field("padding", MapSpacing(fragment.Padding)),
            DiagnosticObject.Field("widthPt", FromNullable(fragment.WidthPt)),
            DiagnosticObject.Field("heightPt", FromNullable(fragment.HeightPt)),
            DiagnosticObject.Field("display", FromNullable(fragment.Display)),
            DiagnosticObject.Field("text", FromNullable(fragment.Text)),
            DiagnosticObject.Field("contentX", FromNullable(fragment.ContentX)),
            DiagnosticObject.Field("contentY", FromNullable(fragment.ContentY)),
            DiagnosticObject.Field("contentSize", MapSize(fragment.ContentSize)),
            DiagnosticObject.Field("occupiedX", FromNullable(fragment.OccupiedX)),
            DiagnosticObject.Field("occupiedY", FromNullable(fragment.OccupiedY)),
            DiagnosticObject.Field("occupiedSize", MapSize(fragment.OccupiedSize)),
            DiagnosticObject.Field("borders", MapBorders(fragment.Borders)),
            DiagnosticObject.Field("displayRole", FromNullable(fragment.DisplayRole)),
            DiagnosticObject.Field("formattingContext", FromNullable(fragment.FormattingContext)),
            DiagnosticObject.Field("markerOffset", FromNullable(fragment.MarkerOffset)),
            DiagnosticObject.Field("derivedColumnCount", FromNullable(fragment.DerivedColumnCount)),
            DiagnosticObject.Field("rowIndex", FromNullable(fragment.RowIndex)),
            DiagnosticObject.Field("columnIndex", FromNullable(fragment.ColumnIndex)),
            DiagnosticObject.Field("isHeader", FromNullable(fragment.IsHeader)),
            DiagnosticObject.Field("metadataOwner", FromNullable(fragment.MetadataOwner)),
            DiagnosticObject.Field("metadataConsumer", FromNullable(fragment.MetadataConsumer)),
            DiagnosticObject.Field("children", new DiagnosticArray(fragment.Children.Select(MapFragmentSnapshot))));
    }

    internal static DiagnosticObject MapSize(SizePt size) =>
        DiagnosticObject.Create(
            DiagnosticObject.Field("width", size.Width),
            DiagnosticObject.Field("height", size.Height));

    internal static DiagnosticObject? MapSize(SizePt? size) =>
        size.HasValue ? MapSize(size.Value) : null;

    internal static DiagnosticObject? MapSpacing(Spacing? spacing)
    {
        if (!spacing.HasValue)
        {
            return null;
        }

        var value = spacing.Value;
        return
            DiagnosticObject.Create(
                DiagnosticObject.Field("top", value.Top),
                DiagnosticObject.Field("right", value.Right),
                DiagnosticObject.Field("bottom", value.Bottom),
                DiagnosticObject.Field("left", value.Left));
    }

    internal static DiagnosticObject? MapBorders(BorderEdges? borders)
    {
        return borders is null
            ? null
            : DiagnosticObject.Create(
                DiagnosticObject.Field("top", MapBorderSide(borders.Top)),
                DiagnosticObject.Field("right", MapBorderSide(borders.Right)),
                DiagnosticObject.Field("bottom", MapBorderSide(borders.Bottom)),
                DiagnosticObject.Field("left", MapBorderSide(borders.Left)));
    }

    private static DiagnosticObject? MapBorderSide(BorderSide? side)
    {
        return side is null
            ? null
            : DiagnosticObject.Create(
                DiagnosticObject.Field("width", side.Width),
                DiagnosticObject.Field("color", side.Color.ToHex()),
                DiagnosticObject.Field("lineStyle", DiagnosticValue.FromEnum(side.LineStyle)));
    }

    private static DiagnosticValue? MapColor(ColorRgba? color) =>
        color.HasValue ? DiagnosticValue.From(color.Value.ToHex()) : null;

    private static DiagnosticValue? FromNullable(string? value) =>
        value is null ? null : DiagnosticValue.From(value);

    private static DiagnosticValue? FromNullable(float? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private static DiagnosticValue? FromNullable(int? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private static DiagnosticValue? FromNullable(bool? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private static DiagnosticValue? FromNullable<TEnum>(TEnum? value)
        where TEnum : struct, Enum =>
        value.HasValue ? DiagnosticValue.FromEnum(value.Value) : null;

    private static FragmentSnapshot MapLineBox(LineBoxFragment line, int sequenceId)
    {
        var text = line.Runs is null
            ? null
            : string.Concat(line.Runs.Select(r => r.Text));

        return CreateSnapshot(
            line,
            sequenceId,
            "line",
            [],
            color: ResolveLineColor(line),
            text: text,
            occupiedX: line.OccupiedRect.X,
            occupiedY: line.OccupiedRect.Y,
            occupiedSize: new SizePt(line.OccupiedRect.Width, line.OccupiedRect.Height));
    }

    private static FragmentSnapshot MapImage(ImageFragment image, int sequenceId)
    {
        return CreateSnapshot(
            image,
            sequenceId,
            "image",
            [],
            contentX: image.ContentRect.X,
            contentY: image.ContentRect.Y,
            contentSize: image.ContentSize);
    }

    private static FragmentSnapshot MapRule(RuleFragment rule, int sequenceId)
    {
        return CreateSnapshot(rule, sequenceId, "rule", []);
    }

    private static FragmentSnapshot MapUnknown(LayoutFragment fragment, int sequenceId)
    {
        return CreateSnapshot(fragment, sequenceId, fragment.GetType().Name.ToLowerInvariant(), []);
    }

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
        return new FragmentSnapshot
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
        bool? isHeader = null)
    {
        return CreateSnapshot(
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
    }

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
                "table",
                children,
                derivedColumnCount: table.DerivedColumnCount);
        }

        private FragmentSnapshot MapTableRow(TableRowFragment row)
        {
            var fragmentSequenceId = NextSequenceId();
            var children = MapFragments(row.Cells);

            return CreateBlockLikeSnapshot(
                row,
                fragmentSequenceId,
                "table-row",
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
                "table-cell",
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
                "block",
                children);
        }

        private int NextSequenceId()
        {
            _sequenceId++;
            return _sequenceId;
        }
    }
}
