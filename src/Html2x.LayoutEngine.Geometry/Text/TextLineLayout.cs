using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

internal sealed class TextLineLayout(ITextMeasurer measurer)
{
    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));

    public TextLayoutResult Layout(TextLayoutInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var builder = new TextLineLayoutState(_measurer, input, NormalizeWidth(input.AvailableWidth));

        foreach (var run in input.Runs)
        {
            builder.ProcessRun(run);
        }

        builder.FlushLine(true);

        var totalHeight = builder.Lines.Sum(line => line.LineHeight);
        var maxWidth = builder.Lines.Count == 0 ? 0f : builder.Lines.Max(l => l.LineWidth);

        return new(builder.Lines, totalHeight, maxWidth);
    }

    private static float NormalizeWidth(float width)
    {
        if (float.IsPositiveInfinity(width))
        {
            return float.PositiveInfinity;
        }

        if (!float.IsFinite(width))
        {
            throw new ArgumentOutOfRangeException(nameof(width),
                "Available width must be finite or positive infinity.");
        }

        if (width < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Available width cannot be negative.");
        }

        return width;
    }
}