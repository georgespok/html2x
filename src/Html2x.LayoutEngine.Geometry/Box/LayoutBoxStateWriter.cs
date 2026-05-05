using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Owns all mutable writes to layout boxes after geometry facts have been resolved.
/// </summary>
internal sealed class LayoutBoxStateWriter
{
    public void ApplyBlockLayout(
        BlockBox block,
        BlockMeasurementBasis measurement,
        UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(block);

        block.Margin = measurement.Margin;
        block.Padding = measurement.Padding;
        block.TextAlign = block.Style.TextAlign;
        block.ApplyLayoutGeometry(geometry);
    }

    public void ApplyInlineLayout(BlockBox block, InlineLayoutResult inlineLayout)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(inlineLayout);

        block.InlineLayout = inlineLayout;
    }

    public void ApplyTextAlignment(BlockBox block)
    {
        ArgumentNullException.ThrowIfNull(block);
        block.TextAlign = block.Style.TextAlign;
    }

    public void ApplyImageBlockLayout(
        ImageBox image,
        BlockMeasurementBasis measurement,
        UsedGeometry geometry,
        ImageLayoutResolution resolution)
    {
        ApplyImageMetadata(image, resolution);
        ApplyBlockLayout(image, measurement, geometry);
    }

    public void ApplyTableLayout(
        TableBox table,
        Spacing margin,
        Spacing padding,
        UsedGeometry geometry,
        int derivedColumnCount)
    {
        ArgumentNullException.ThrowIfNull(table);

        table.Margin = margin;
        table.Padding = padding;
        table.TextAlign = table.Style.TextAlign;
        table.DerivedColumnCount = derivedColumnCount;
        table.ApplyLayoutGeometry(geometry);
    }

    public void ApplyUnsupportedTablePlaceholder(
        TableBox table,
        Spacing margin,
        UsedGeometry geometry)
    {
        ApplyTableLayout(
            table,
            margin,
            table.Style.Padding.Safe(),
            geometry,
            derivedColumnCount: 0);
        table.Children.Clear();
    }

    public void ApplyTableRowLayout(
        TableRowBox row,
        int rowIndex,
        UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(row);

        row.Margin = row.Style.Margin.Safe();
        row.Padding = row.Style.Padding.Safe();
        row.RowIndex = rowIndex;
        row.TextAlign = row.Style.TextAlign;
        row.ApplyLayoutGeometry(geometry);
    }

    public void ApplyTableCellLayout(
        TableCellBox cell,
        int columnIndex,
        bool isHeader,
        UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(cell);

        cell.Margin = cell.Style.Margin.Safe();
        cell.Padding = cell.Style.Padding.Safe();
        cell.ColumnIndex = columnIndex;
        cell.IsHeader = isHeader;
        cell.TextAlign = cell.Style.TextAlign;
        cell.ApplyLayoutGeometry(geometry);
    }

    public void ApplyInlineObjectContentLayout(
        BlockBox contentBox,
        Spacing margin,
        Spacing padding,
        UsedGeometry geometry,
        InlineLayoutResult inlineLayout,
        ImageLayoutResolution? imageResolution = null)
    {
        ArgumentNullException.ThrowIfNull(contentBox);
        ArgumentNullException.ThrowIfNull(inlineLayout);

        contentBox.Margin = margin;
        contentBox.Padding = padding;
        contentBox.TextAlign = contentBox.Style.TextAlign;
        contentBox.ApplyLayoutGeometry(geometry);
        contentBox.InlineLayout = inlineLayout;

        if (contentBox is ImageBox imageBox && imageResolution is { } resolvedImage)
        {
            ApplyImageMetadata(imageBox, resolvedImage);
        }
    }

    private static void ApplyImageMetadata(ImageBox image, ImageLayoutResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(resolution);

        image.ApplyImageMetadata(
            resolution.Src,
            resolution.AuthoredSizePx,
            resolution.IntrinsicSizePx,
            resolution.Status);
    }
}
