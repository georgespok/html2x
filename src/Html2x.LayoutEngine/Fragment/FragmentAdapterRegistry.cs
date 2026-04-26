using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Models;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Fragment;

internal interface IBlockFragmentAdapter
{
    bool CanCreate(BlockBox source);

    BlockFragment Create(BlockBox source, FragmentBuildState state);
}

internal interface ISpecialFragmentAdapter
{
    bool CanCreate(DisplayNode source);

    LayoutFragment Create(DisplayNode source, FragmentBuildState state);
}

internal sealed class FragmentAdapterRegistry
{
    internal const string MetadataOwnerName = nameof(FragmentAdapterRegistry);

    private readonly IReadOnlyList<IBlockFragmentAdapter> _blockAdapters;
    private readonly IReadOnlyList<ISpecialFragmentAdapter> _specialAdapters;

    public FragmentAdapterRegistry(
        IEnumerable<IBlockFragmentAdapter> blockAdapters,
        IEnumerable<ISpecialFragmentAdapter> specialAdapters)
    {
        ArgumentNullException.ThrowIfNull(blockAdapters);
        ArgumentNullException.ThrowIfNull(specialAdapters);
        _blockAdapters = blockAdapters.ToArray();
        _specialAdapters = specialAdapters.ToArray();
    }

    public static FragmentAdapterRegistry CreateDefault()
    {
        return new FragmentAdapterRegistry(
            CreateDefaultBlockAdapters(),
            CreateDefaultSpecialAdapters());
    }

    internal static IReadOnlyList<IBlockFragmentAdapter> CreateDefaultBlockAdapters()
    {
        return
        [
            new TableFragmentAdapter(),
            new TableRowFragmentAdapter(),
            new TableCellFragmentAdapter(),
            new DefaultBlockFragmentAdapter()
        ];
    }

    internal static IReadOnlyList<ISpecialFragmentAdapter> CreateDefaultSpecialAdapters()
    {
        return
        [
            new RuleFragmentAdapter(),
            new ImageFragmentAdapter()
        ];
    }

    public BlockFragment CreateBlockFragment(BlockBox source, FragmentBuildState state)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(state);

        var adapter = _blockAdapters.FirstOrDefault(candidate => candidate.CanCreate(source));
        if (adapter is null)
        {
            throw new InvalidOperationException(
                $"No block fragment adapter registered for '{source.GetType().Name}'.");
        }

        return adapter.Create(source, state);
    }

    public bool TryCreateSpecialFragment(
        DisplayNode source,
        FragmentBuildState state,
        out LayoutFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(state);

        var adapter = _specialAdapters.FirstOrDefault(candidate => candidate.CanCreate(source));
        if (adapter is null)
        {
            fragment = null!;
            return false;
        }

        fragment = adapter.Create(source, state);
        return true;
    }

    private sealed class TableFragmentAdapter : IBlockFragmentAdapter
    {
        public bool CanCreate(BlockBox source) => source.Role == DisplayRole.Table;

        public BlockFragment Create(BlockBox source, FragmentBuildState state)
        {
            var metadata = CreateMetadata(source);

            return new TableFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = CreateRect(source),
                Style = StyleConverter.FromComputed(source.Style),
                DisplayRole = metadata.DisplayRole,
                FormattingContext = metadata.FormattingContext,
                MarkerOffset = metadata.MarkerOffset,
                DerivedColumnCount = metadata.DerivedColumnCount ?? 0
            };
        }
    }

    private sealed class TableRowFragmentAdapter : IBlockFragmentAdapter
    {
        public bool CanCreate(BlockBox source) => source.Role == DisplayRole.TableRow;

        public BlockFragment Create(BlockBox source, FragmentBuildState state)
        {
            var metadata = CreateMetadata(source);

            return new TableRowFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = CreateRect(source),
                Style = StyleConverter.FromComputed(source.Style),
                DisplayRole = metadata.DisplayRole,
                FormattingContext = metadata.FormattingContext,
                MarkerOffset = metadata.MarkerOffset,
                RowIndex = metadata.RowIndex ?? 0
            };
        }
    }

    private sealed class TableCellFragmentAdapter : IBlockFragmentAdapter
    {
        public bool CanCreate(BlockBox source) => source.Role == DisplayRole.TableCell;

        public BlockFragment Create(BlockBox source, FragmentBuildState state)
        {
            var metadata = CreateMetadata(source);

            return new TableCellFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = CreateRect(source),
                Style = StyleConverter.FromComputed(source.Style),
                DisplayRole = metadata.DisplayRole,
                FormattingContext = metadata.FormattingContext,
                MarkerOffset = metadata.MarkerOffset,
                ColumnIndex = metadata.ColumnIndex ?? 0,
                IsHeader = metadata.IsHeader == true
            };
        }
    }

    private sealed class DefaultBlockFragmentAdapter : IBlockFragmentAdapter
    {
        public bool CanCreate(BlockBox source) => true;

        public BlockFragment Create(BlockBox source, FragmentBuildState state)
        {
            var metadata = CreateMetadata(source);

            return new BlockFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = CreateRect(source),
                Style = StyleConverter.FromComputed(source.Style),
                DisplayRole = metadata.DisplayRole,
                FormattingContext = metadata.FormattingContext,
                MarkerOffset = metadata.MarkerOffset
            };
        }
    }

    private sealed class RuleFragmentAdapter : ISpecialFragmentAdapter
    {
        public bool CanCreate(DisplayNode source) => source is RuleBox;

        public LayoutFragment Create(DisplayNode source, FragmentBuildState state)
        {
            var box = (RuleBox)source;
            var geometry = RequireGeometry(box);
            return new RuleFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = geometry.BorderBoxRect,
                Style = StyleConverter.FromComputed(box.Style)
            };
        }
    }

    private sealed class ImageFragmentAdapter : ISpecialFragmentAdapter
    {
        public bool CanCreate(DisplayNode source) => source is ImageBox;

        public LayoutFragment Create(DisplayNode source, FragmentBuildState state)
        {
            var imageBox = (ImageBox)source;
            var geometry = RequireGeometry(imageBox);

            return new ImageFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Src = imageBox.Src,
                AuthoredSizePx = imageBox.AuthoredSizePx,
                IntrinsicSizePx = imageBox.IntrinsicSizePx,
                IsMissing = imageBox.IsMissing,
                IsOversize = imageBox.IsOversize,
                Rect = geometry.BorderBoxRect,
                ContentRect = geometry.ContentBoxRect,
                Style = StyleConverter.FromComputed(imageBox.Style)
            };
        }
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

        if (box.Role != DisplayRole.Table)
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
        if (box.Role != DisplayRole.TableRow)
        {
            return null;
        }

        return box is TableRowBox rowBox && rowBox.RowIndex >= 0
            ? rowBox.RowIndex
            : ResolveSiblingIndex(box, DisplayRole.TableRow);
    }

    private static int? ResolveColumnIndex(BlockBox box)
    {
        if (box.Role != DisplayRole.TableCell)
        {
            return null;
        }

        return box is TableCellBox cellBox && cellBox.ColumnIndex >= 0
            ? cellBox.ColumnIndex
            : ResolveSiblingIndex(box, DisplayRole.TableCell);
    }

    private static bool? ResolveIsHeader(BlockBox box)
    {
        if (box.Role != DisplayRole.TableCell)
        {
            return null;
        }

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
            $"Fragment creation requires UsedGeometry for '{DisplayNodePathBuilder.Build(box)}'.");
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

    private sealed record FragmentMetadata(
        FragmentDisplayRole DisplayRole,
        FormattingContextKind FormattingContext,
        float? MarkerOffset,
        int? DerivedColumnCount,
        int? RowIndex,
        int? ColumnIndex,
        bool? IsHeader);
}
