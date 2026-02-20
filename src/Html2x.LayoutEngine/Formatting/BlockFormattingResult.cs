using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Formatting;

internal sealed record BlockFormattingResult
{
    public BlockFormattingResult(
        IReadOnlyList<BlockBox> formattedBlocks,
        float totalWidth,
        float totalHeight,
        float? baseline = null)
    {
        FormattedBlocks = formattedBlocks ?? throw new ArgumentNullException(nameof(formattedBlocks));
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

    public IReadOnlyList<BlockBox> FormattedBlocks { get; }

    public float TotalWidth { get; }

    public float TotalHeight { get; }

    public float? Baseline { get; }

    public static BlockFormattingResult Empty { get; } =
        new BlockFormattingResult(Array.Empty<BlockBox>(), 0f, 0f);

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
