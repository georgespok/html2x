namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Calculates border-box height from non-mutating block content measurement.
/// </summary>
internal sealed class BlockContentHeightMeasurement(BlockContentSizeMeasurement measurer)
{
    private readonly BlockContentSizeMeasurement _measurer =
        measurer ?? throw new ArgumentNullException(nameof(measurer));

    public float MeasureBorderBoxHeight(
        BlockBox block,
        float availableWidth,
        Func<TableBox, float, float> measureTableHeight)
    {
        ArgumentNullException.ThrowIfNull(measureTableHeight);

        return _measurer.Measure(
                block,
                availableWidth,
                (table, tableAvailableWidth) => BlockContentSizeFacts.ForBorderBoxHeight(
                    measureTableHeight(table, tableAvailableWidth)))
            .BorderBoxHeight;
    }
}