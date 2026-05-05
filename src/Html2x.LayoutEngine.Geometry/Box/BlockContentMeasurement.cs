using Html2x.LayoutEngine.Geometry.Primitives;

namespace Html2x.LayoutEngine.Geometry.Box;


/// <summary>
/// Carries pure block content measurement facts without requiring temporary box mutation.
/// </summary>
internal readonly record struct BlockContentMeasurement
{
    public BlockContentMeasurement(
        float borderBoxHeight,
        float contentHeight,
        float inlineHeight,
        float nestedBlockHeight,
        ImageLayoutResolution? image = null,
        TableLayoutResult? table = null)
    {
        BorderBoxHeight = UsedGeometryCalculator.RequireNonNegativeFinite(borderBoxHeight);
        ContentHeight = UsedGeometryCalculator.RequireNonNegativeFinite(contentHeight);
        InlineHeight = UsedGeometryCalculator.RequireNonNegativeFinite(inlineHeight);
        NestedBlockHeight = UsedGeometryCalculator.RequireNonNegativeFinite(nestedBlockHeight);
        Image = image;
        Table = table;
    }

    public float BorderBoxHeight { get; }

    public float ContentHeight { get; }

    public float InlineHeight { get; }

    public float NestedBlockHeight { get; }

    public ImageLayoutResolution? Image { get; }

    public TableLayoutResult? Table { get; }

    public static BlockContentMeasurement ForBorderBoxHeight(float borderBoxHeight)
    {
        var height = Math.Max(0f, borderBoxHeight);
        return new BlockContentMeasurement(
            height,
            contentHeight: height,
            inlineHeight: 0f,
            nestedBlockHeight: 0f);
    }

    public static BlockContentMeasurement ForImage(ImageLayoutResolution image)
    {
        return new BlockContentMeasurement(
            image.TotalHeight,
            image.ContentHeight,
            inlineHeight: 0f,
            nestedBlockHeight: 0f,
            image: image);
    }

    public static BlockContentMeasurement ForTable(TableLayoutResult table)
    {
        ArgumentNullException.ThrowIfNull(table);

        return new BlockContentMeasurement(
            table.BorderBoxHeight,
            table.ContentHeight,
            inlineHeight: 0f,
            nestedBlockHeight: 0f,
            table: table);
    }
}
