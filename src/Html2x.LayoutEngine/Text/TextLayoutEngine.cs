using Html2x.Abstractions.Layout.Text;
using System.Linq;

namespace Html2x.LayoutEngine.Text;

internal sealed class TextLayoutEngine(ITextMeasurer measurer)
{
    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));

    public TextLayoutResult Layout(TextLayoutInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var builder = new TextLayoutLineBuilder(_measurer, input, NormalizeWidth(input.AvailableWidth));

        foreach (var run in input.Runs)
        {
            builder.ProcessRun(run);
        }

        builder.FlushLine(forceWhenEmpty: true);

        var totalHeight = builder.Lines.Sum(line => line.LineHeight);
        var maxWidth = builder.Lines.Count == 0 ? 0f : builder.Lines.Max(l => l.LineWidth);

        return new TextLayoutResult(builder.Lines, totalHeight, maxWidth);
    }

    private static float NormalizeWidth(float width)
    {
        if (!float.IsFinite(width) || width <= 0f)
        {
            return float.PositiveInfinity;
        }

        return width;
    }

}
