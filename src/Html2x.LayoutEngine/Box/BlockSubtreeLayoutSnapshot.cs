using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

internal sealed class BlockSubtreeLayoutSnapshot
{
    private readonly IReadOnlyList<BlockSnapshot> _blocks;

    private BlockSubtreeLayoutSnapshot(IReadOnlyList<BlockSnapshot> blocks)
    {
        _blocks = blocks;
    }

    public static BlockSubtreeLayoutSnapshot Capture(BlockBox root)
    {
        var blocks = EnumerateBlocks(root)
            .Select(static block => BlockSnapshot.Capture(block))
            .ToArray();

        return new BlockSubtreeLayoutSnapshot(blocks);
    }

    public void Restore()
    {
        foreach (var snapshot in _blocks)
        {
            snapshot.Restore();
        }
    }

    private static IEnumerable<BlockBox> EnumerateBlocks(BoxNode node)
    {
        if (node is BlockBox block)
        {
            yield return block;
        }

        foreach (var child in node.Children)
        {
            foreach (var nested in EnumerateBlocks(child))
            {
                yield return nested;
            }
        }
    }

    private sealed class BlockSnapshot
    {
        private readonly BlockBox _block;
        private readonly BlockBoxLayoutState _layoutState;
        private readonly ImageSnapshot? _image;
        private readonly int? _derivedColumnCount;
        private readonly int? _rowIndex;
        private readonly CellSnapshot? _cell;

        private BlockSnapshot(
            BlockBox block,
            BlockBoxLayoutState layoutState,
            ImageSnapshot? image,
            int? derivedColumnCount,
            int? rowIndex,
            CellSnapshot? cell)
        {
            _block = block;
            _layoutState = layoutState;
            _image = image;
            _derivedColumnCount = derivedColumnCount;
            _rowIndex = rowIndex;
            _cell = cell;
        }

        public static BlockSnapshot Capture(BlockBox block)
        {
            return new BlockSnapshot(
                block,
                block.CaptureLayoutState(),
                block is ImageBox image ? ImageSnapshot.Capture(image) : null,
                block is TableBox table ? table.DerivedColumnCount : null,
                block is TableRowBox row ? row.RowIndex : null,
                block is TableCellBox cell ? CellSnapshot.Capture(cell) : null);
        }

        public void Restore()
        {
            _block.RestoreLayoutState(_layoutState);

            if (_image is not null && _block is ImageBox image)
            {
                _image.Restore(image);
            }

            if (_derivedColumnCount.HasValue && _block is TableBox table)
            {
                table.DerivedColumnCount = _derivedColumnCount.Value;
            }

            if (_rowIndex.HasValue && _block is TableRowBox row)
            {
                row.RowIndex = _rowIndex.Value;
            }

            if (_cell is not null && _block is TableCellBox cell)
            {
                _cell.Restore(cell);
            }
        }
    }

    private sealed record ImageSnapshot(
        string Src,
        SizePx AuthoredSizePx,
        SizePx IntrinsicSizePx,
        bool IsMissing,
        bool IsOversize)
    {
        public static ImageSnapshot Capture(ImageBox image)
        {
            return new ImageSnapshot(
                image.Src,
                image.AuthoredSizePx,
                image.IntrinsicSizePx,
                image.IsMissing,
                image.IsOversize);
        }

        public void Restore(ImageBox image)
        {
            image.Src = Src;
            image.AuthoredSizePx = AuthoredSizePx;
            image.IntrinsicSizePx = IntrinsicSizePx;
            image.IsMissing = IsMissing;
            image.IsOversize = IsOversize;
        }
    }

    private sealed record CellSnapshot(int ColumnIndex, bool IsHeader)
    {
        public static CellSnapshot Capture(TableCellBox cell)
        {
            return new CellSnapshot(cell.ColumnIndex, cell.IsHeader);
        }

        public void Restore(TableCellBox cell)
        {
            cell.ColumnIndex = ColumnIndex;
            cell.IsHeader = IsHeader;
        }
    }
}
