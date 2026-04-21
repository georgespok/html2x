using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Measurements.Units;
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

        var sequenceId = 0;

        return new GeometrySnapshot
        {
            Fragments = LayoutSnapshotMapper.From(layout),
            Boxes = MapBoxes(boxTree.Blocks, ref sequenceId),
            Pagination = pagination.Pages.Select(MapPage).ToList()
        };
    }

    private static IReadOnlyList<BoxGeometrySnapshot> MapBoxes(
        IEnumerable<BlockBox> boxes,
        ref int sequenceId)
    {
        var snapshots = new List<BoxGeometrySnapshot>();
        foreach (var box in boxes)
        {
            snapshots.Add(MapBox(box, ref sequenceId));
        }

        return snapshots;
    }

    private static BoxGeometrySnapshot MapBox(BlockBox box, ref int sequenceId)
    {
        var geometry = box.UsedGeometry ?? throw new InvalidOperationException(
            $"Geometry snapshot requires UsedGeometry for '{DisplayNodePathBuilder.Build(box)}'.");
        var borderRect = geometry.BorderBoxRect;
        var contentRect = geometry.ContentBoxRect;

        return new BoxGeometrySnapshot
        {
            SequenceId = NextSequenceId(ref sequenceId),
            Path = DisplayNodePathBuilder.Build(box),
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
            Children = MapBoxes(DisplayNodeTraversal.EnumerateBlockChildren(box), ref sequenceId)
        };
    }

    private static PaginationPageSnapshot MapPage(PageModel page)
    {
        return new PaginationPageSnapshot
        {
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            Margin = page.Margins,
            ContentTop = page.ContentTop,
            ContentBottom = page.ContentBottom,
            Placements = page.Placements.Select(MapPlacement).ToList()
        };
    }

    private static PaginationPlacementSnapshot MapPlacement(BlockFragmentPlacement placement)
    {
        return new PaginationPlacementSnapshot
        {
            FragmentId = placement.FragmentId,
            Kind = placement.Fragment.DisplayRole?.ToString() ?? placement.Fragment.GetType().Name,
            PageNumber = placement.PageNumber,
            OrderIndex = placement.OrderIndex,
            IsOversized = placement.IsOversized,
            X = placement.LocalX,
            Y = placement.LocalY,
            Size = new SizePt(placement.Width, placement.Height)
        };
    }

    private static int NextSequenceId(ref int sequenceId)
    {
        sequenceId++;
        return sequenceId;
    }
}
