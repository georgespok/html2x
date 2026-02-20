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

        return new BlockFragment
        {
            FragmentId = state.ReserveFragmentId(),
            PageNumber = state.PageNumber,
            Rect = rect,
            Style = style,
            DisplayRole = displayRole,
            FormattingContext = formattingContext,
            MarkerOffset = markerOffset
        };
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
