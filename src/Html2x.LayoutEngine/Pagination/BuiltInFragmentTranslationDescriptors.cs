using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Geometry;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
/// Provides translation descriptors for every built-in fragment type emitted by the layout engine.
/// </summary>
internal static class BuiltInFragmentTranslationDescriptors
{
    private static readonly IReadOnlyList<FragmentTranslationDescriptor> Descriptors =
    [
        FragmentTranslationDescriptor.Create<BlockFragment>(
            [nameof(LayoutFragment.Rect), nameof(BlockFragment.Children)],
            FragmentChildTraversalPolicy.Children,
            TranslateBlock),
        FragmentTranslationDescriptor.Create<TableFragment>(
            [nameof(LayoutFragment.Rect), nameof(TableFragment.Rows)],
            FragmentChildTraversalPolicy.Rows,
            TranslateTable),
        FragmentTranslationDescriptor.Create<TableRowFragment>(
            [nameof(LayoutFragment.Rect), nameof(TableRowFragment.Cells)],
            FragmentChildTraversalPolicy.Cells,
            TranslateTableRow),
        FragmentTranslationDescriptor.Create<TableCellFragment>(
            [nameof(LayoutFragment.Rect), nameof(TableCellFragment.Children)],
            FragmentChildTraversalPolicy.Children,
            TranslateTableCell),
        FragmentTranslationDescriptor.Create<LineBoxFragment>(
            [
                nameof(LayoutFragment.Rect),
                nameof(LineBoxFragment.OccupiedRect),
                nameof(LineBoxFragment.BaselineY),
                $"{nameof(LineBoxFragment.Runs)}.{nameof(TextRun.Origin)}"
            ],
            FragmentChildTraversalPolicy.None,
            TranslateLine),
        FragmentTranslationDescriptor.Create<ImageFragment>(
            [nameof(LayoutFragment.Rect), nameof(ImageFragment.ContentRect)],
            FragmentChildTraversalPolicy.None,
            TranslateImage),
        FragmentTranslationDescriptor.Create<RuleFragment>(
            [nameof(LayoutFragment.Rect)],
            FragmentChildTraversalPolicy.None,
            TranslateRule)
    ];

    public static IReadOnlyList<FragmentTranslationDescriptor> Create()
    {
        return Descriptors.ToArray();
    }

    private static LayoutFragment TranslateBlock(
        BlockFragment source,
        FragmentTranslationRequest request,
        IFragmentTranslator translator)
    {
        return new BlockFragment(
            source.Children.Select(child => translator.Translate(child, request)))
        {
            FragmentId = source.FragmentId,
            PageNumber = request.PageNumber,
            Rect = GeometryTranslator.Translate(source.Rect, request.DeltaX, request.DeltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset
        };
    }

    private static LayoutFragment TranslateTable(
        TableFragment source,
        FragmentTranslationRequest request,
        IFragmentTranslator translator)
    {
        return new TableFragment(
            source.Rows.Select(row => (TableRowFragment)translator.Translate(row, request)))
        {
            FragmentId = source.FragmentId,
            PageNumber = request.PageNumber,
            Rect = GeometryTranslator.Translate(source.Rect, request.DeltaX, request.DeltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset,
            DerivedColumnCount = source.DerivedColumnCount
        };
    }

    private static LayoutFragment TranslateTableRow(
        TableRowFragment source,
        FragmentTranslationRequest request,
        IFragmentTranslator translator)
    {
        return new TableRowFragment(
            source.Cells.Select(cell => (TableCellFragment)translator.Translate(cell, request)))
        {
            FragmentId = source.FragmentId,
            PageNumber = request.PageNumber,
            Rect = GeometryTranslator.Translate(source.Rect, request.DeltaX, request.DeltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset,
            RowIndex = source.RowIndex
        };
    }

    private static LayoutFragment TranslateTableCell(
        TableCellFragment source,
        FragmentTranslationRequest request,
        IFragmentTranslator translator)
    {
        return new TableCellFragment(
            source.Children.Select(child => translator.Translate(child, request)))
        {
            FragmentId = source.FragmentId,
            PageNumber = request.PageNumber,
            Rect = GeometryTranslator.Translate(source.Rect, request.DeltaX, request.DeltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            DisplayRole = source.DisplayRole,
            FormattingContext = source.FormattingContext,
            MarkerOffset = source.MarkerOffset,
            ColumnIndex = source.ColumnIndex,
            IsHeader = source.IsHeader
        };
    }

    private static LayoutFragment TranslateLine(
        LineBoxFragment source,
        FragmentTranslationRequest request,
        IFragmentTranslator _)
    {
        return new LineBoxFragment
        {
            FragmentId = source.FragmentId,
            PageNumber = request.PageNumber,
            Rect = GeometryTranslator.Translate(source.Rect, request.DeltaX, request.DeltaY),
            OccupiedRect = GeometryTranslator.Translate(source.OccupiedRect, request.DeltaX, request.DeltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            BaselineY = source.BaselineY + request.DeltaY,
            LineHeight = source.LineHeight,
            Runs = source.Runs
                .Select(run => GeometryTranslator.Translate(run, request.DeltaX, request.DeltaY))
                .ToList(),
            TextAlign = source.TextAlign
        };
    }

    private static LayoutFragment TranslateImage(
        ImageFragment source,
        FragmentTranslationRequest request,
        IFragmentTranslator _)
    {
        return new ImageFragment
        {
            FragmentId = source.FragmentId,
            PageNumber = request.PageNumber,
            Rect = GeometryTranslator.Translate(source.Rect, request.DeltaX, request.DeltaY),
            ZOrder = source.ZOrder,
            Style = source.Style,
            Src = source.Src,
            ContentRect = GeometryTranslator.Translate(source.ContentRect, request.DeltaX, request.DeltaY),
            AuthoredSizePx = source.AuthoredSizePx,
            IntrinsicSizePx = source.IntrinsicSizePx,
            IsMissing = source.IsMissing,
            IsOversize = source.IsOversize
        };
    }

    private static LayoutFragment TranslateRule(
        RuleFragment source,
        FragmentTranslationRequest request,
        IFragmentTranslator _)
    {
        return new RuleFragment
        {
            FragmentId = source.FragmentId,
            PageNumber = request.PageNumber,
            Rect = GeometryTranslator.Translate(source.Rect, request.DeltaX, request.DeltaY),
            ZOrder = source.ZOrder,
            Style = source.Style
        };
    }
}
