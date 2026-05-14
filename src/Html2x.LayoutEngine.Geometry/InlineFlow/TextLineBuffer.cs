using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

/// <summary>
///     Tracks the mutable current text line while inline text is wrapped.
/// </summary>
internal sealed class TextLineBuffer
{
    private readonly float _availableWidth;
    private readonly float _lineHeight;
    private readonly TextLineMeasurement _lineMeasurement;
    private readonly ITextMeasurer _measurer;
    private readonly List<TextLineRunBuffer> _runs = [];
    private float _currentWidth;

    public TextLineBuffer(ITextMeasurer measurer, float lineHeight, float availableWidth)
    {
        _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
        _lineMeasurement = new(_measurer);
        _lineHeight = lineHeight;
        _availableWidth = availableWidth;
    }

    public bool HasContent => _runs.Count > 0;

    public bool TryAppendText(TextRunInput run, string text)
    {
        var textWidth = MeasureWidth(run, text);
        if (!Fits(_currentWidth + textWidth + GetAdditionalSpacing(run), _availableWidth))
        {
            return false;
        }

        AppendText(run, text, textWidth);
        return true;
    }

    public void AppendText(TextRunInput run, string text) =>
        AppendText(run, text, MeasureWidth(run, text));

    public bool TryAppendInlineBox(TextRunInput run)
    {
        if (run.InlineBox is null)
        {
            return false;
        }

        var inlineBoxWidth = run.InlineBox.BorderBoxWidth;
        if (!Fits(_currentWidth + inlineBoxWidth + GetAdditionalSpacing(run), _availableWidth))
        {
            return false;
        }

        AppendInlineBox(run, inlineBoxWidth);
        return true;
    }

    public void AppendInlineBox(TextRunInput run)
    {
        if (run.InlineBox is null)
        {
            return;
        }

        AppendInlineBox(run, run.InlineBox.BorderBoxWidth);
    }

    public TextLayoutLine? Flush(bool forceWhenEmpty = false)
    {
        if (_runs.Count == 0)
        {
            return forceWhenEmpty ? new([], 0f, _lineHeight) : null;
        }

        TrimLineEnd(_runs);

        var line = _lineMeasurement.Measure(_runs, _lineHeight);
        _runs.Clear();
        _currentWidth = 0f;

        return line;
    }

    private void AppendText(TextRunInput run, string text, float textWidth)
    {
        var buffer = GetOrCreateTextBuffer(run);
        buffer.Append(text);
        _currentWidth += textWidth;
    }

    private TextLineRunBuffer GetOrCreateTextBuffer(TextRunInput run)
    {
        if (_runs.Count > 0 && _runs[^1].Source.RunId == run.RunId)
        {
            return _runs[^1];
        }

        var buffer = new TextLineRunBuffer(run);
        _runs.Add(buffer);
        _currentWidth += buffer.LeftSpacing + buffer.RightSpacing;
        return buffer;
    }

    private void AppendInlineBox(TextRunInput run, float inlineBoxWidth)
    {
        var buffer = new TextLineRunBuffer(run, run.InlineBox);
        _runs.Add(buffer);
        _currentWidth += buffer.LeftSpacing + inlineBoxWidth + buffer.RightSpacing;
    }

    private float MeasureWidth(TextRunInput run, string text) =>
        _measurer.Measure(run.Font, run.FontSizePt, text).Width;

    private float GetAdditionalSpacing(TextRunInput run)
    {
        if (_runs.Count == 0)
        {
            return run.PaddingLeft + run.MarginLeft + run.PaddingRight + run.MarginRight;
        }

        var buffer = _runs[^1];
        if (buffer.Source.RunId == run.RunId)
        {
            return 0f;
        }

        return run.PaddingLeft + run.MarginLeft + run.PaddingRight + run.MarginRight;
    }

    private static bool Fits(float width, float maxWidth)
    {
        if (float.IsPositiveInfinity(maxWidth))
        {
            return true;
        }

        if (maxWidth <= 0f)
        {
            return false;
        }

        return width <= maxWidth;
    }

    private static void TrimLineEnd(List<TextLineRunBuffer> runs)
    {
        for (var i = runs.Count - 1; i >= 0; i--)
        {
            var buffer = runs[i];
            if (buffer.InlineBox is not null)
            {
                return;
            }

            if (buffer.Text.Length == 0)
            {
                runs.RemoveAt(i);
                continue;
            }

            var trimmed = buffer.Text.ToString().TrimEnd();
            if (trimmed.Length == buffer.Text.Length)
            {
                return;
            }

            buffer.Text.Clear();
            buffer.Text.Append(trimmed);

            if (buffer.Text.Length == 0)
            {
                runs.RemoveAt(i);
            }

            return;
        }
    }
}
