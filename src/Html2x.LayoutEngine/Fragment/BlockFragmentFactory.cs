using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

internal static class BlockFragmentFactory
{
    public static BlockFragment Create(BlockBox box, FragmentBuildState state)
    {
        return new BlockFragment
        {
            FragmentId = state.ReserveFragmentId(),
            PageNumber = state.PageNumber,
            Rect = new RectangleF(box.X, box.Y, box.Width, box.Height),
            Style = StyleConverter.FromComputed(box.Style),
            DisplayRole = MapRole(box.Role),
            FormattingContext = box.IsInlineBlockContext
                ? FormattingContextKind.InlineBlock
                : FormattingContextKind.Block,
            MarkerOffset = box.MarkerOffset > 0f ? box.MarkerOffset : null
        };
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
