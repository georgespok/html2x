using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Models;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Fragment;

internal sealed class BoxToFragmentProjector
{
    internal const string MetadataOwnerName = nameof(FragmentBuilder);

    public BlockFragment CreateBlockFragment(BlockBox source, int fragmentId, int pageNumber)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Role switch
        {
            BoxRole.Table => CreateTableFragment(source, fragmentId, pageNumber),
            BoxRole.TableRow => CreateTableRowFragment(source, fragmentId, pageNumber),
            BoxRole.TableCell => CreateTableCellFragment(source, fragmentId, pageNumber),
            _ => CreateStandardBlockFragment(source, fragmentId, pageNumber)
        };
    }

    public bool TryCreateSpecialFragment(
        BoxNode source,
        int fragmentId,
        int pageNumber,
        out LayoutFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(source);

        switch (source)
        {
            case RuleBox rule:
                fragment = CreateRuleFragment(rule, fragmentId, pageNumber);
                return true;
            case ImageBox image:
                fragment = CreateImageFragment(image, fragmentId, pageNumber);
                return true;
            default:
                fragment = null!;
                return false;
        }
    }

    private static TableFragment CreateTableFragment(BlockBox source, int fragmentId, int pageNumber)
    {
        var metadata = CreateMetadata(source);

        return new TableFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Rect = CreateRect(source),
            Style = StyleConverter.FromComputed(source.Style),
            DisplayRole = metadata.DisplayRole,
            FormattingContext = metadata.FormattingContext,
            MarkerOffset = metadata.MarkerOffset,
            DerivedColumnCount = metadata.DerivedColumnCount ?? 0
        };
    }

    private static TableRowFragment CreateTableRowFragment(BlockBox source, int fragmentId, int pageNumber)
    {
        var metadata = CreateMetadata(source);

        return new TableRowFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Rect = CreateRect(source),
            Style = StyleConverter.FromComputed(source.Style),
            DisplayRole = metadata.DisplayRole,
            FormattingContext = metadata.FormattingContext,
            MarkerOffset = metadata.MarkerOffset,
            RowIndex = metadata.RowIndex ?? 0
        };
    }

    private static TableCellFragment CreateTableCellFragment(BlockBox source, int fragmentId, int pageNumber)
    {
        var metadata = CreateMetadata(source);

        return new TableCellFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Rect = CreateRect(source),
            Style = StyleConverter.FromComputed(source.Style),
            DisplayRole = metadata.DisplayRole,
            FormattingContext = metadata.FormattingContext,
            MarkerOffset = metadata.MarkerOffset,
            ColumnIndex = metadata.ColumnIndex ?? 0,
            IsHeader = metadata.IsHeader == true
        };
    }

    private static BlockFragment CreateStandardBlockFragment(BlockBox source, int fragmentId, int pageNumber)
    {
        var metadata = CreateMetadata(source);

        return new BlockFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Rect = CreateRect(source),
            Style = StyleConverter.FromComputed(source.Style),
            DisplayRole = metadata.DisplayRole,
            FormattingContext = metadata.FormattingContext,
            MarkerOffset = metadata.MarkerOffset
        };
    }

    private static RuleFragment CreateRuleFragment(RuleBox source, int fragmentId, int pageNumber)
    {
        var geometry = RequireGeometry(source);
        return new RuleFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Rect = geometry.BorderBoxRect,
            Style = StyleConverter.FromComputed(source.Style)
        };
    }

    private static ImageFragment CreateImageFragment(ImageBox source, int fragmentId, int pageNumber)
    {
        var geometry = RequireGeometry(source);

        return new ImageFragment
        {
            FragmentId = fragmentId,
            PageNumber = pageNumber,
            Src = source.Src,
            AuthoredSizePx = source.AuthoredSizePx,
            IntrinsicSizePx = source.IntrinsicSizePx,
            IsMissing = source.IsMissing,
            IsOversize = source.IsOversize,
            Rect = geometry.BorderBoxRect,
            ContentRect = geometry.ContentBoxRect,
            Style = StyleConverter.FromComputed(source.Style)
        };
    }

    private static FragmentMetadata CreateMetadata(BlockBox box)
    {
        return new FragmentMetadata(
            MapRole(box.Role),
            ResolveFormattingContext(box),
            ResolveMarkerOffset(box),
            ResolveDerivedColumnCount(box),
            ResolveRowIndex(box),
            ResolveColumnIndex(box),
            ResolveIsHeader(box));
    }

    private static int? ResolveDerivedColumnCount(BlockBox box)
    {
        if (box is TableBox tableBox && tableBox.DerivedColumnCount >= 0)
        {
            return tableBox.DerivedColumnCount;
        }

        if (box.Role != BoxRole.Table)
        {
            return null;
        }

        return box.Children
            .OfType<BlockBox>()
            .Select(static row => row.Children.OfType<BlockBox>().Count())
            .DefaultIfEmpty()
            .Max();
    }

    private static int? ResolveRowIndex(BlockBox box)
    {
        if (box.Role != BoxRole.TableRow)
        {
            return null;
        }

        return box is TableRowBox rowBox && rowBox.RowIndex >= 0
            ? rowBox.RowIndex
            : ResolveSiblingIndex(box, BoxRole.TableRow);
    }

    private static int? ResolveColumnIndex(BlockBox box)
    {
        if (box.Role != BoxRole.TableCell)
        {
            return null;
        }

        return box is TableCellBox cellBox && cellBox.ColumnIndex >= 0
            ? cellBox.ColumnIndex
            : ResolveSiblingIndex(box, BoxRole.TableCell);
    }

    private static bool? ResolveIsHeader(BlockBox box)
    {
        if (box.Role != BoxRole.TableCell)
        {
            return null;
        }

        return (box as TableCellBox)?.IsHeader == true
               || string.Equals(box.Element?.TagName, "th", StringComparison.OrdinalIgnoreCase);
    }

    private static int ResolveSiblingIndex(BlockBox box, BoxRole role)
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
        return RequireGeometry(box).BorderBoxRect;
    }

    private static FormattingContextKind ResolveFormattingContext(BlockBox box)
    {
        return box.IsInlineBlockContext
            ? FormattingContextKind.InlineBlock
            : FormattingContextKind.Block;
    }

    private static float? ResolveMarkerOffset(BlockBox box)
    {
        var markerOffset = RequireGeometry(box).MarkerOffset;
        return markerOffset > 0f ? markerOffset : null;
    }

    private static UsedGeometry RequireGeometry(BlockBox box)
    {
        return box.UsedGeometry ?? throw new InvalidOperationException(
            $"Fragment creation requires UsedGeometry for '{BoxNodePathBuilder.Build(box)}'.");
    }

    private static FragmentDisplayRole MapRole(BoxRole role)
    {
        return role switch
        {
            BoxRole.Block => FragmentDisplayRole.Block,
            BoxRole.Inline => FragmentDisplayRole.Inline,
            BoxRole.InlineBlock => FragmentDisplayRole.InlineBlock,
            BoxRole.ListItem => FragmentDisplayRole.ListItem,
            BoxRole.Table => FragmentDisplayRole.Table,
            BoxRole.TableRow => FragmentDisplayRole.TableRow,
            BoxRole.TableCell => FragmentDisplayRole.TableCell,
            _ => FragmentDisplayRole.Block
        };
    }

    private sealed record FragmentMetadata(
        FragmentDisplayRole DisplayRole,
        FormattingContextKind FormattingContext,
        float? MarkerOffset,
        int? DerivedColumnCount,
        int? RowIndex,
        int? ColumnIndex,
        bool? IsHeader);
}
