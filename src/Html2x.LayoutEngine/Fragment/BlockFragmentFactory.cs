using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

internal static class BlockFragmentFactory
{
    public static BlockFragment Create(BlockBox box, FragmentBuildState state)
    {
        var rect = CreateRect(box);
        var style = StyleConverter.FromComputed(box.Style);
        var displayRole = MapRole(box.Role);
        var formattingContext = ResolveFormattingContext(box);
        var markerOffset = ResolveMarkerOffset(box);

        return box.Role switch
        {
            DisplayRole.Table => new TableFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = rect,
                Style = style,
                DisplayRole = displayRole,
                FormattingContext = formattingContext,
                MarkerOffset = markerOffset,
                DerivedColumnCount = ResolveDerivedColumnCount(box)
            },
            DisplayRole.TableRow => new TableRowFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = rect,
                Style = style,
                DisplayRole = displayRole,
                FormattingContext = formattingContext,
                MarkerOffset = markerOffset,
                RowIndex = ResolveRowIndex(box)
            },
            DisplayRole.TableCell => new TableCellFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = rect,
                Style = style,
                DisplayRole = displayRole,
                FormattingContext = formattingContext,
                MarkerOffset = markerOffset,
                ColumnIndex = ResolveColumnIndex(box),
                IsHeader = ResolveIsHeader(box)
            },
            _ => new BlockFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = rect,
                Style = style,
                DisplayRole = displayRole,
                FormattingContext = formattingContext,
                MarkerOffset = markerOffset
            }
        };
    }

    private static int ResolveDerivedColumnCount(BlockBox box)
    {
        if (box is TableBox tableBox && tableBox.DerivedColumnCount >= 0)
        {
            return tableBox.DerivedColumnCount;
        }

        return box.Children
            .OfType<BlockBox>()
            .Select(static row => row.Children.OfType<BlockBox>().Count())
            .DefaultIfEmpty()
            .Max();
    }

    private static int ResolveRowIndex(BlockBox box)
    {
        return box is TableRowBox rowBox && rowBox.RowIndex >= 0
            ? rowBox.RowIndex
            : ResolveSiblingIndex(box, DisplayRole.TableRow);
    }

    private static int ResolveColumnIndex(BlockBox box)
    {
        return box is TableCellBox cellBox && cellBox.ColumnIndex >= 0
            ? cellBox.ColumnIndex
            : ResolveSiblingIndex(box, DisplayRole.TableCell);
    }

    private static bool ResolveIsHeader(BlockBox box)
    {
        return (box as TableCellBox)?.IsHeader == true
            || string.Equals(box.Element?.TagName, "th", StringComparison.OrdinalIgnoreCase);
    }

    private static int ResolveSiblingIndex(BlockBox box, DisplayRole role)
    {
        if (box.Parent is null)
        {
            return 0;
        }

        return box.Parent.Children
            .TakeWhile(child => !ReferenceEquals(child, box))
            .Count(child => child is BlockBox sibling && sibling.Role == role);
    }

    private static RectangleF CreateRect(BlockBox box)
    {
        return new RectangleF(box.X, box.Y, box.Width, box.Height);
    }

    private static FormattingContextKind ResolveFormattingContext(BlockBox box)
    {
        return box.IsInlineBlockContext
            ? FormattingContextKind.InlineBlock
            : FormattingContextKind.Block;
    }

    private static float? ResolveMarkerOffset(BlockBox box)
    {
        return box.MarkerOffset > 0f ? box.MarkerOffset : null;
    }

    private static FragmentDisplayRole MapRole(DisplayRole role)
    {
        return role switch
        {
            DisplayRole.Block => FragmentDisplayRole.Block,
            DisplayRole.Inline => FragmentDisplayRole.Inline,
            DisplayRole.InlineBlock => FragmentDisplayRole.InlineBlock,
            DisplayRole.ListItem => FragmentDisplayRole.ListItem,
            DisplayRole.Table => FragmentDisplayRole.Table,
            DisplayRole.TableRow => FragmentDisplayRole.TableRow,
            DisplayRole.TableCell => FragmentDisplayRole.TableCell,
            _ => FragmentDisplayRole.Block
        };
    }
}
