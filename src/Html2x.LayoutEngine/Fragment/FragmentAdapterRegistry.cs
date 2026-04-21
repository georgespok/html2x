using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
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
            return new TableFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = CreateRect(source),
                Style = StyleConverter.FromComputed(source.Style),
                DisplayRole = MapRole(source.Role),
                FormattingContext = ResolveFormattingContext(source),
                MarkerOffset = ResolveMarkerOffset(source),
                DerivedColumnCount = ResolveDerivedColumnCount(source)
            };
        }
    }

    private sealed class TableRowFragmentAdapter : IBlockFragmentAdapter
    {
        public bool CanCreate(BlockBox source) => source.Role == DisplayRole.TableRow;

        public BlockFragment Create(BlockBox source, FragmentBuildState state)
        {
            return new TableRowFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = CreateRect(source),
                Style = StyleConverter.FromComputed(source.Style),
                DisplayRole = MapRole(source.Role),
                FormattingContext = ResolveFormattingContext(source),
                MarkerOffset = ResolveMarkerOffset(source),
                RowIndex = ResolveRowIndex(source)
            };
        }
    }

    private sealed class TableCellFragmentAdapter : IBlockFragmentAdapter
    {
        public bool CanCreate(BlockBox source) => source.Role == DisplayRole.TableCell;

        public BlockFragment Create(BlockBox source, FragmentBuildState state)
        {
            return new TableCellFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = CreateRect(source),
                Style = StyleConverter.FromComputed(source.Style),
                DisplayRole = MapRole(source.Role),
                FormattingContext = ResolveFormattingContext(source),
                MarkerOffset = ResolveMarkerOffset(source),
                ColumnIndex = ResolveColumnIndex(source),
                IsHeader = ResolveIsHeader(source)
            };
        }
    }

    private sealed class DefaultBlockFragmentAdapter : IBlockFragmentAdapter
    {
        public bool CanCreate(BlockBox source) => true;

        public BlockFragment Create(BlockBox source, FragmentBuildState state)
        {
            return new BlockFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = CreateRect(source),
                Style = StyleConverter.FromComputed(source.Style),
                DisplayRole = MapRole(source.Role),
                FormattingContext = ResolveFormattingContext(source),
                MarkerOffset = ResolveMarkerOffset(source)
            };
        }
    }

    private sealed class RuleFragmentAdapter : ISpecialFragmentAdapter
    {
        public bool CanCreate(DisplayNode source) => source is RuleBox;

        public LayoutFragment Create(DisplayNode source, FragmentBuildState state)
        {
            var box = (RuleBox)source;
            return new RuleFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Rect = box.UsedGeometry?.BorderBoxRect ?? new RectangleF(box.X, box.Y, box.Width, box.Height),
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
            var geometry = imageBox.UsedGeometry;
            var outerRect = geometry?.BorderBoxRect ?? new RectangleF(imageBox.X, imageBox.Y, imageBox.Width, imageBox.Height);
            var contentRect = geometry?.ContentBoxRect ?? outerRect;

            return new ImageFragment
            {
                FragmentId = state.ReserveFragmentId(),
                PageNumber = state.PageNumber,
                Src = imageBox.Src,
                AuthoredSizePx = imageBox.AuthoredSizePx,
                IntrinsicSizePx = imageBox.IntrinsicSizePx,
                IsMissing = imageBox.IsMissing,
                IsOversize = imageBox.IsOversize,
                Rect = outerRect,
                ContentRect = contentRect,
                Style = StyleConverter.FromComputed(imageBox.Style)
            };
        }
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
        return box.UsedGeometry?.BorderBoxRect ?? new RectangleF(box.X, box.Y, box.Width, box.Height);
    }

    private static FormattingContextKind ResolveFormattingContext(BlockBox box)
    {
        return box.IsInlineBlockContext
            ? FormattingContextKind.InlineBlock
            : FormattingContextKind.Block;
    }

    private static float? ResolveMarkerOffset(BlockBox box)
    {
        var markerOffset = box.UsedGeometry?.MarkerOffset ?? box.MarkerOffset;
        return markerOffset > 0f ? markerOffset : null;
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
