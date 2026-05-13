namespace Html2x.LayoutEngine.Geometry.Measurement;

internal sealed record BlockFormattingMetricsResult
{
    public BlockFormattingMetricsResult(
        IReadOnlyList<BlockBox> measuredBlocks,
        float totalWidth,
        float totalHeight,
        float? baseline = null)
    {
        MeasuredBlocks = measuredBlocks ?? throw new ArgumentNullException(nameof(measuredBlocks));
        TotalWidth = totalWidth;
        TotalHeight = totalHeight;
        Baseline = baseline;

        ValidateMetric(totalWidth, nameof(totalWidth));
        ValidateMetric(totalHeight, nameof(totalHeight));

        if (baseline.HasValue)
        {
            ValidateMetric(baseline.Value, nameof(baseline));
        }
    }

    public IReadOnlyList<BlockBox> MeasuredBlocks { get; }

    public float TotalWidth { get; }

    public float TotalHeight { get; }

    public float? Baseline { get; }

    public static BlockFormattingMetricsResult Empty { get; } = new([], 0f, 0f);

    private static void ValidateMetric(float value, string name)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, "Metric values must be finite.");
        }

        if (value < 0f)
        {
            throw new ArgumentOutOfRangeException(name, "Metric values cannot be negative.");
        }
    }
}
