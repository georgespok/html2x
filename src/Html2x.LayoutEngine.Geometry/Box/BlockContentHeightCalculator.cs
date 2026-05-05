namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Calculates border-box height from non-mutating block content measurement.
/// </summary>
internal sealed class BlockContentHeightCalculator(BlockContentMeasurer measurer)
{
    private readonly BlockContentMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));

    public float MeasureBorderBoxHeight(
        BlockBox block,
        float availableWidth,
        Func<TableBox, float, float> measureTableHeight)
    {
        ArgumentNullException.ThrowIfNull(measureTableHeight);

        return _measurer.Measure(
            block,
            availableWidth,
            (table, tableAvailableWidth) => BlockContentMeasurement.ForBorderBoxHeight(
                measureTableHeight(table, tableAvailableWidth)))
            .BorderBoxHeight;
    }
}
