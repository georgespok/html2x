namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Applies mutable layout state to internal boxes in one place.
/// </summary>
internal static class BlockLayoutState
{
    private static readonly LayoutBoxStateWriter Writer = new();

    public static void Apply(BlockBox node, BlockMeasurementBasis measurement, UsedGeometry geometry)
    {
        Writer.ApplyBlockLayout(node, measurement, geometry);
    }
}
