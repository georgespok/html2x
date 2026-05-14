using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

/// <summary>
///     Builds wrapped text layout lines from measured inline run inputs.
/// </summary>
internal sealed class TextLineLayoutState(ITextMeasurer measurer, TextLayoutInput input, float availableWidth)
{
    private readonly TextLineBuffer _line =
        new(measurer, (input ?? throw new ArgumentNullException(nameof(input))).LineHeight, availableWidth);
    private readonly List<TextLayoutLine> _lines = [];

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
            case TextRunKind.InlineBox:
                AppendInlineBox(run);
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
        var line = _line.Flush(forceWhenEmpty);
        if (line is not null)
        {
            _lines.Add(line);
        }
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
        var lineBreaker = new TokenLineBreaker(this, run, token);
        lineBreaker.Execute();
    }

    private void AppendAtomicToken(TextRunInput run, string token)
    {
        if (string.IsNullOrWhiteSpace(token) && !_line.HasContent)
        {
            return;
        }

        if (_line.TryAppendText(run, token))
        {
            return;
        }

        if (_line.HasContent)
        {
            FlushLine();
        }

        _line.AppendText(run, token);
    }

    private void AppendInlineBox(TextRunInput run)
    {
        if (run.InlineBox is null)
        {
            return;
        }

        if (_line.TryAppendInlineBox(run))
        {
            return;
        }

        if (_line.HasContent)
        {
            FlushLine();
        }

        _line.AppendInlineBox(run);
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
            _line.AppendText(run, element);
        }
    }

    private bool TryAppendToken(TextRunInput run, string token)
    {
        if (_line.TryAppendText(run, token))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Processes one token against the current line buffer.
    /// </summary>
    private readonly struct TokenLineBreaker(TextLineLayoutState state, TextRunInput run, string token)
    {
        private readonly TextLineLayoutState _state = state;
        private readonly TextRunInput _run = run;
        private readonly string _token = token;

        public void Execute()
        {
            if (IsLeadingWhitespace())
            {
                return;
            }

            if (_state.TryAppendToken(_run, _token))
            {
                return;
            }

            _state.FlushLine();

            if (string.IsNullOrWhiteSpace(_token))
            {
                return;
            }

            if (_state.TryAppendToken(_run, _token))
            {
                return;
            }

            _state.ProcessTokenByGrapheme(_run, _token);
        }

        private bool IsLeadingWhitespace() => string.IsNullOrWhiteSpace(_token) && !_state._line.HasContent;
    }
}
