using Html2x.LayoutEngine.Geometry.Primitives;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Carries pure block content measurement facts without requiring temporary box mutation.
/// </summary>
internal readonly record struct BlockContentSizeFacts
{
    public BlockContentSizeFacts(
        float borderBoxHeight,
        float contentHeight,
        float inlineHeight,
        float nestedBlockHeight,
        ImageLayoutResolution? image = null,
        TableLayoutResult? table = null)
    {
        BorderBoxHeight = UsedGeometryRules.RequireNonNegativeFinite(borderBoxHeight);
        ContentHeight = UsedGeometryRules.RequireNonNegativeFinite(contentHeight);
        InlineHeight = UsedGeometryRules.RequireNonNegativeFinite(inlineHeight);
        NestedBlockHeight = UsedGeometryRules.RequireNonNegativeFinite(nestedBlockHeight);
        Image = image;
        Table = table;
    }

    public float BorderBoxHeight { get; }

    public float ContentHeight { get; }

    public float InlineHeight { get; }

    public float NestedBlockHeight { get; }

    public ImageLayoutResolution? Image { get; }

    public TableLayoutResult? Table { get; }

    public static BlockContentSizeFacts ForBorderBoxHeight(float borderBoxHeight)
    {
        var height = Math.Max(0f, borderBoxHeight);
        return new(
            height,
            height,
            0f,
            0f);
    }

    public static BlockContentSizeFacts ForImage(ImageLayoutResolution image) =>
        new(
            image.TotalHeight,
            image.ContentHeight,
            0f,
            0f,
            image);

    public static BlockContentSizeFacts ForTable(TableLayoutResult table)
    {
        ArgumentNullException.ThrowIfNull(table);

        return new(
            table.BorderBoxHeight,
            table.ContentHeight,
            0f,
            0f,
            table: table);
    }
}