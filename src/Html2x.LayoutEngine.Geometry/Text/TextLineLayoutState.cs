using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Builds wrapped text layout lines from measured inline run inputs.
/// </summary>
internal sealed class TextLineLayoutState(ITextMeasurer measurer, TextLayoutInput input, float availableWidth)
{
    private readonly float _availableWidth = availableWidth;
    private readonly List<TextLineRunBuffer> _currentLine = [];
    private readonly TextLayoutInput _input = input ?? throw new ArgumentNullException(nameof(input));
    private readonly TextLineMeasurement _lineMeasurement = new(measurer);
    private readonly List<TextLayoutLine> _lines = [];
    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
    private float _currentWidth;

    public IReadOnlyList<TextLayoutLine> Lines => _lines;

    public void ProcessRun(TextRunInput? run)
    {
        if (run is null)
        {
            return;
        }

        switch (run.Kind)
        {
            case TextRunKind.LineBreak:
                FlushLine(true);
                return;
            case TextRunKind.Atomic:
                ProcessRunLines(run, AppendAtomicToken);
                return;
            case TextRunKind.InlineObject:
                AppendInlineObject(run);
                return;
            case TextRunKind.Normal:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(run.Kind), run.Kind, "Unsupported run kind.");
        }

        ProcessRunLines(run, ProcessLogicalLine);
    }

    private void ProcessRunLines(TextRunInput run, Action<TextRunInput, string> tokenHandler)
    {
        if (string.IsNullOrEmpty(run.Text))
        {
            return;
        }

        var isFirstLine = true;
        foreach (var rawLine in TextTokenization.SplitIntoLogicalLines(run.Text))
        {
            if (!isFirstLine)
            {
                FlushLine(true);
            }

            if (rawLine.Length == 0)
            {
                FlushLine(true);
                isFirstLine = false;
                continue;
            }

            tokenHandler(run, rawLine);
            isFirstLine = false;
        }
    }

    public void FlushLine(bool forceWhenEmpty = false)
    {
        if (_currentLine.Count == 0)
        {
            if (forceWhenEmpty)
            {
                _lines.Add(new([], 0f, _input.LineHeight));
            }

            return;
        }

        TrimLineEnd(_currentLine);

        _lines.Add(_lineMeasurement.Measure(_currentLine, _input.LineHeight));

        _currentLine.Clear();
        _currentWidth = 0f;
    }

    private void ProcessLogicalLine(TextRunInput run, string rawLine)
    {
        foreach (var token in TextTokenization.Tokenize(rawLine))
        {
            ProcessToken(run, token);
        }
    }

    private void ProcessToken(TextRunInput run, string token)
    {
        var processor = new TokenProcessor(this, run, token);
        processor.Execute();
    }

    private void AppendAtomicToken(TextRunInput run, string token)
    {
        if (string.IsNullOrWhiteSpace(token) && _currentLine.Count == 0)
        {
            return;
        }

        var tokenWidth = MeasureWidth(run, token);
        var additionalSpacing = GetAdditionalSpacing(run);
        if (!Fits(_currentWidth + tokenWidth + additionalSpacing, _availableWidth) && _currentLine.Count > 0)
        {
            FlushLine();
        }

        AppendToken(run, token, tokenWidth);
    }

    private void AppendInlineObject(TextRunInput run)
    {
        if (run.InlineObject is null)
        {
            return;
        }

        var tokenWidth = run.InlineObject.BorderBoxWidth;
        var additionalSpacing = GetAdditionalSpacing(run);
        if (!Fits(_currentWidth + tokenWidth + additionalSpacing, _availableWidth) && _currentLine.Count > 0)
        {
            FlushLine();
        }

        AppendInlineObject(run, tokenWidth);
    }

    private void ProcessTokenByGrapheme(TextRunInput run, string token)
    {
        foreach (var element in TextTokenization.EnumerateGraphemes(token))
        {
            if (TryAppendToken(run, element))
            {
                continue;
            }

            FlushLine();
            AppendToken(run, element, MeasureWidth(run, element));
        }
    }

    private bool TryAppendToken(TextRunInput run, string token)
    {
        var tokenWidth = MeasureWidth(run, token);
        var additionalSpacing = GetAdditionalSpacing(run);
        if (Fits(_currentWidth + tokenWidth + additionalSpacing, _availableWidth))
        {
            AppendToken(run, token, tokenWidth);
            return true;
        }

        return false;
    }

    private void AppendToken(TextRunInput run, string token, float tokenWidth)
    {
        var buffer = GetOrCreateTextBuffer(run);
        buffer.Append(token);
        _currentWidth += tokenWidth;
    }

    private TextLineRunBuffer GetOrCreateTextBuffer(TextRunInput run)
    {
        if (_currentLine.Count > 0 && _currentLine[^1].Source.RunId == run.RunId)
        {
            return _currentLine[^1];
        }

        var buffer = new TextLineRunBuffer(run);
        _currentLine.Add(buffer);
        _currentWidth += buffer.LeftSpacing + buffer.RightSpacing;
        return buffer;
    }

    private void AppendInlineObject(TextRunInput run, float tokenWidth)
    {
        var buffer = new TextLineRunBuffer(run, run.InlineObject);
        _currentLine.Add(buffer);
        _currentWidth += buffer.LeftSpacing + tokenWidth + buffer.RightSpacing;
    }

    private float MeasureWidth(TextRunInput run, string text) => _measurer.MeasureWidth(run.Font, run.FontSizePt, text);

    private float GetAdditionalSpacing(TextRunInput run)
    {
        if (_currentLine.Count == 0)
        {
            return run.PaddingLeft + run.MarginLeft + run.PaddingRight + run.MarginRight;
        }

        var buffer = _currentLine[^1];
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
            if (buffer.InlineObject is not null)
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

    /// <summary>
    ///     Processes one token against the current line buffer.
    /// </summary>
    private readonly struct TokenProcessor(TextLineLayoutState builder, TextRunInput run, string token)
    {
        private readonly TextLineLayoutState _builder = builder;
        private readonly TextRunInput _run = run;
        private readonly string _token = token;

        public void Execute()
        {
            if (IsLeadingWhitespace())
            {
                return;
            }

            if (_builder.TryAppendToken(_run, _token))
            {
                return;
            }

            _builder.FlushLine();

            if (string.IsNullOrWhiteSpace(_token))
            {
                return;
            }

            if (_builder.TryAppendToken(_run, _token))
            {
                return;
            }

            _builder.ProcessTokenByGrapheme(_run, _token);
        }

        private bool IsLeadingWhitespace() => string.IsNullOrWhiteSpace(_token) && _builder._currentLine.Count == 0;
    }
}