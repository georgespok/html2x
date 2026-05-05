using Html2x.RenderModel.Fragments;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.LayoutEngine.Pagination;

internal sealed class FragmentPlacementCloner
{
    public LayoutFragment CloneWithPlacement(LayoutFragment source, int pageNumber, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Clone(source, pageNumber, x - source.Rect.X, y - source.Rect.Y);
    }

    public BlockFragment CloneBlockWithPlacement(BlockFragment source, int pageNumber, float x, float y)
    {
        return (BlockFragment)CloneWithPlacement(source, pageNumber, x, y);
    }

    private static LayoutFragment Clone(LayoutFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return source switch
        {
            TableFragment table => CloneTable(table, pageNumber, deltaX, deltaY),
            TableRowFragment row => CloneTableRow(row, pageNumber, deltaX, deltaY),
            TableCellFragment cell => CloneTableCell(cell, pageNumber, deltaX, deltaY),
            BlockFragment block => CloneBlock(block, pageNumber, deltaX, deltaY),
            LineBoxFragment line => CloneLine(line, pageNumber, deltaX, deltaY),
            ImageFragment image => CloneImage(image, pageNumber, deltaX, deltaY),
            RuleFragment rule => CloneRule(rule, pageNumber, deltaX, deltaY),
            _ => throw new NotSupportedException(
                $"Pagination cannot clone fragment type '{source.GetType().Name}'. Add an explicit clone branch for the new rendering fragment.")
        };
    }

    private static BlockFragment CloneBlock(BlockFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new BlockFragment(source.Children.Select(child => Clone(child, pageNumber, deltaX, deltaY)))
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = source.Rect.Translate(deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset
        };
    }

    private static TableFragment CloneTable(TableFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new TableFragment(source.Rows.Select(row => (TableRowFragment)Clone(row, pageNumber, deltaX, deltaY)))
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = source.Rect.Translate(deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset,
            DerivedColumnCount = source.DerivedColumnCount
        };
    }

    private static TableRowFragment CloneTableRow(TableRowFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new TableRowFragment(source.Cells.Select(cell => (TableCellFragment)Clone(cell, pageNumber, deltaX, deltaY)))
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = source.Rect.Translate(deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset,
            RowIndex = source.RowIndex
        };
    }

    private static TableCellFragment CloneTableCell(TableCellFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new TableCellFragment(source.Children.Select(child => Clone(child, pageNumber, deltaX, deltaY)))
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = source.Rect.Translate(deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset,
            ColumnIndex = source.ColumnIndex,
            IsHeader = source.IsHeader
        };
    }

    private static LineBoxFragment CloneLine(LineBoxFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new LineBoxFragment
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = source.Rect.Translate(deltaX, deltaY),
            OccupiedRect = source.OccupiedRect.Translate(deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            BaselineY = source.BaselineY + deltaY,
            LineHeight = source.LineHeight,
            Runs = source.Runs
                .Select(run => run with { Origin = run.Origin.Translate(deltaX, deltaY) })
                .ToList(),
            TextAlign = source.TextAlign
        };
    }

    private static ImageFragment CloneImage(ImageFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new ImageFragment
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = source.Rect.Translate(deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            Src = source.Src,
            ContentRect = source.ContentRect.Translate(deltaX, deltaY),
            AuthoredSizePx = source.AuthoredSizePx,
            IntrinsicSizePx = source.IntrinsicSizePx,
            Status = source.Status
        };
    }

    private static RuleFragment CloneRule(RuleFragment source, int pageNumber, float deltaX, float deltaY)
    {
        return new RuleFragment
        {
            FragmentId = source.FragmentId,
            PageNumber = pageNumber,
            Rect = source.Rect.Translate(deltaX, deltaY),
            ZOrder = source.ZOrder,
            Style = source.Style
        };
    }
}
