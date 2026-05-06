using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Converts buffered line content into measured layout runs.
/// </summary>
internal sealed class TextLineMeasurement(ITextMeasurer measurer)
{
    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));

    public TextLayoutLine Measure(IReadOnlyList<TextLineRunBuffer> buffers, float fallbackLineHeight)
    {
        var lineRuns = new List<TextLayoutRun>(buffers.Count);
        var lineWidth = 0f;

        foreach (var buffer in buffers)
        {
            if (buffer.InlineBox is not null)
            {
                var inlineRun = BuildInlineBoxRun(buffer);
                lineRuns.Add(inlineRun);
                lineWidth += buffer.LeftSpacing + inlineRun.Width + buffer.RightSpacing;
                continue;
            }

            if (buffer.Text.Length == 0)
            {
                continue;
            }

            var textRun = BuildTextRun(buffer);
            lineRuns.Add(textRun);
            lineWidth += buffer.LeftSpacing + textRun.Width + buffer.RightSpacing;
        }

        return new(
            lineRuns,
            lineWidth,
            ResolveLineHeight(lineRuns, fallbackLineHeight));
    }

    private TextLayoutRun BuildInlineBoxRun(TextLineRunBuffer buffer)
    {
        var inlineBox = buffer.InlineBox
                           ?? throw new InvalidOperationException(
                               "Inline box buffer must carry inline box layout.");
        var inlineAscent = inlineBox.Baseline;
        var inlineDescent = inlineBox.BorderBoxHeight - inlineBox.Baseline;

        return new(
            buffer.Source.Source,
            string.Empty,
            buffer.Source.Font,
            buffer.Source.FontSizePt,
            inlineBox.BorderBoxWidth,
            buffer.LeftSpacing,
            buffer.RightSpacing,
            inlineAscent,
            inlineDescent,
            buffer.Source.Style.Decorations,
            buffer.Source.Style.Color,
            null,
            inlineBox);
    }

    private TextLayoutRun BuildTextRun(TextLineRunBuffer buffer)
    {
        var text = buffer.Text.ToString();
        var measurement = _measurer.Measure(buffer.Source.Font, buffer.Source.FontSizePt, text);

        return new(
            buffer.Source.Source,
            text,
            buffer.Source.Font,
            buffer.Source.FontSizePt,
            measurement.Width,
            buffer.LeftSpacing,
            buffer.RightSpacing,
            measurement.Ascent,
            measurement.Descent,
            buffer.Source.Style.Decorations,
            buffer.Source.Style.Color,
            measurement.ResolvedFont);
    }

    private static float ResolveLineHeight(IReadOnlyList<TextLayoutRun> runs, float fallbackLineHeight)
    {
        var maxAscent = 0f;
        var maxDescent = 0f;

        foreach (var run in runs)
        {
            maxAscent = Math.Max(maxAscent, run.Ascent);
            maxDescent = Math.Max(maxDescent, run.Descent);
        }

        var measured = maxAscent + maxDescent;
        if (measured <= 0f)
        {
            return fallbackLineHeight;
        }

        return Math.Max(fallbackLineHeight, measured);
    }
}
