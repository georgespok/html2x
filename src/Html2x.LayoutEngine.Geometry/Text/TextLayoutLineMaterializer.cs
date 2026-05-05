using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
/// Converts buffered line content into measured layout runs.
/// </summary>
internal sealed class TextLayoutLineMaterializer(ITextMeasurer measurer)
{
    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));

    public TextLayoutLine Build(IReadOnlyList<TextLineRunBuffer> buffers, float fallbackLineHeight)
    {
        var lineRuns = new List<TextLayoutRun>(buffers.Count);
        var lineWidth = 0f;

        foreach (var buffer in buffers)
        {
            if (buffer.InlineObject is not null)
            {
                var inlineRun = BuildInlineObjectRun(buffer);
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

        return new TextLayoutLine(
            lineRuns,
            lineWidth,
            ResolveLineHeight(lineRuns, fallbackLineHeight));
    }

    private TextLayoutRun BuildInlineObjectRun(TextLineRunBuffer buffer)
    {
        var inlineObject = buffer.InlineObject
            ?? throw new InvalidOperationException("Inline object buffer must carry inline object layout.");
        var inlineAscent = inlineObject.Baseline;
        var inlineDescent = inlineObject.BorderBoxHeight - inlineObject.Baseline;

        return new TextLayoutRun(
            buffer.Source.Source,
            string.Empty,
            buffer.Source.Font,
            buffer.Source.FontSizePt,
            inlineObject.BorderBoxWidth,
            buffer.LeftSpacing,
            buffer.RightSpacing,
            inlineAscent,
            inlineDescent,
            buffer.Source.Style.Decorations,
            buffer.Source.Style.Color,
            ResolvedFont: null,
            inlineObject);
    }

    private TextLayoutRun BuildTextRun(TextLineRunBuffer buffer)
    {
        var text = buffer.Text.ToString();
        var measurement = _measurer.Measure(buffer.Source.Font, buffer.Source.FontSizePt, text);

        return new TextLayoutRun(
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
