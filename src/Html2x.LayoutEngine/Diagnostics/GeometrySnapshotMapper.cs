using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Pagination;

namespace Html2x.LayoutEngine.Diagnostics;

internal static class GeometrySnapshotMapper
{
    public static GeometrySnapshot From(
        BoxTree boxTree,
        HtmlLayout layout,
        PaginationResult pagination)
    {
        ArgumentNullException.ThrowIfNull(boxTree);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(pagination);

        var boxMapper = new BoxGeometrySnapshotMapper();

        return new GeometrySnapshot
        {
            Fragments = LayoutSnapshotMapper.From(layout),
            Boxes = boxMapper.MapBoxes(boxTree.Blocks),
            Pagination = pagination.Pages.Select(MapPage).ToList()
        };
    }

    private static PaginationPageSnapshot MapPage(PageModel page)
    {
        return new PaginationPageSnapshot
        {
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            Margin = page.Margin,
            ContentTop = page.ContentTop,
            ContentBottom = page.ContentBottom,
            Placements = page.Placements.Select(MapPlacement).ToList()
        };
    }

    private static PaginationPlacementSnapshot MapPlacement(BlockFragmentPlacement placement)
    {
        var fragment = placement.Fragment;

        return new PaginationPlacementSnapshot
        {
            FragmentId = placement.FragmentId,
            Kind = fragment.DisplayRole?.ToString() ?? fragment.GetType().Name,
            PageNumber = placement.PageNumber,
            OrderIndex = placement.OrderIndex,
            IsOversized = placement.IsOversized,
            X = placement.PageX,
            Y = placement.PageY,
            Size = new SizePt(placement.Width, placement.Height),
            DisplayRole = fragment.DisplayRole,
            FormattingContext = fragment.FormattingContext,
            MarkerOffset = fragment.MarkerOffset,
            DerivedColumnCount = fragment is TableFragment table ? table.DerivedColumnCount : null,
            RowIndex = fragment is TableRowFragment row ? row.RowIndex : null,
            ColumnIndex = fragment is TableCellFragment cell ? cell.ColumnIndex : null,
            IsHeader = fragment is TableCellFragment headerCell ? headerCell.IsHeader : null,
            MetadataOwner = BoxToFragmentProjector.MetadataOwnerName,
            MetadataConsumer = nameof(FragmentPlacementCloner)
        };
    }

    private sealed class BoxGeometrySnapshotMapper
    {
        private int _sequenceId;

        public IReadOnlyList<BoxGeometrySnapshot> MapBoxes(IEnumerable<BlockBox> boxes)
        {
            var snapshots = new List<BoxGeometrySnapshot>();
            foreach (var box in boxes)
            {
                snapshots.Add(MapBox(box));
            }

            return snapshots;
        }

        private BoxGeometrySnapshot MapBox(BlockBox box)
        {
            var geometry = box.UsedGeometry ?? throw new InvalidOperationException(
                $"Geometry snapshot requires UsedGeometry for '{BoxNodePathBuilder.Build(box)}'.");
            var borderRect = geometry.BorderBoxRect;
            var contentRect = geometry.ContentBoxRect;

            return new BoxGeometrySnapshot
            {
                SequenceId = NextSequenceId(),
                Path = BoxNodePathBuilder.Build(box),
                Kind = box.Role.ToString().ToLowerInvariant(),
                TagName = box.Element?.TagName?.ToLowerInvariant(),
                X = borderRect.X,
                Y = borderRect.Y,
                Size = new SizePt(borderRect.Width, borderRect.Height),
                ContentX = contentRect.X,
                ContentY = contentRect.Y,
                ContentSize = new SizePt(contentRect.Width, contentRect.Height),
                Baseline = geometry.Baseline,
                MarkerOffset = geometry.MarkerOffset,
                AllowsOverflow = geometry.AllowsOverflow,
                IsAnonymous = box.IsAnonymous,
                IsInlineBlockContext = box.IsInlineBlockContext,
                DerivedColumnCount = box is TableBox tableBox && tableBox.DerivedColumnCount >= 0
                    ? tableBox.DerivedColumnCount
                    : null,
                RowIndex = box is TableRowBox rowBox && rowBox.RowIndex >= 0
                    ? rowBox.RowIndex
                    : null,
                ColumnIndex = box is TableCellBox cellBox && cellBox.ColumnIndex >= 0
                    ? cellBox.ColumnIndex
                    : null,
                IsHeader = box is TableCellBox headerCell ? headerCell.IsHeader : null,
                MetadataOwner = nameof(BlockLayoutEngine),
                MetadataConsumer = nameof(GeometrySnapshotMapper),
                Children = MapBoxes(BoxNodeTraversal
                    .EnumerateBlockChildren(box)
                    .Where(static child => !InlineFlowClassifier.IsInlineFlowMember(child)))
            };
        }

        private int NextSequenceId()
        {
            _sequenceId++;
            return _sequenceId;
        }
    }
}
