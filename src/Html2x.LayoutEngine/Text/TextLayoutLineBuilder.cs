using System.Text;
using Html2x.Abstractions.Layout.Text;

namespace Html2x.LayoutEngine.Text;

internal sealed class TextLayoutLineBuilder(ITextMeasurer measurer, TextLayoutInput input, float availableWidth)
{
    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
    private readonly TextLayoutInput _input = input ?? throw new ArgumentNullException(nameof(input));
    private readonly float _availableWidth = availableWidth;
    private readonly List<TextLayoutLine> _lines = [];
    private readonly List<LineRunBuffer> _currentLine = [];
    private float _currentWidth;

    public IReadOnlyList<TextLayoutLine> Lines => _lines;

    public void ProcessRun(TextRunInput? run)
    {
        if (run is null)
        {
            return;
        }

        if (run.IsLineBreak)
        {
            FlushLine(forceWhenEmpty: true);
            return;
        }

        if (string.IsNullOrEmpty(run.Text))
        {
            return;
        }

        var isFirstLine = true;
        foreach (var rawLine in TextTokenization.SplitIntoLogicalLines(run.Text))
        {
            if (!isFirstLine)
            {
                FlushLine(forceWhenEmpty: true);
            }

            if (rawLine.Length == 0)
            {
                FlushLine(forceWhenEmpty: true);
                isFirstLine = false;
                continue;
            }

            ProcessLogicalLine(run, rawLine);
            isFirstLine = false;
        }
    }

    public void FlushLine(bool forceWhenEmpty = false)
    {
        if (_currentLine.Count == 0)
        {
            if (forceWhenEmpty)
            {
                _lines.Add(new TextLayoutLine([], 0f, _input.LineHeight));
            }

            return;
        }

        TrimLineEnd(_currentLine);

        var lineRuns = BuildLineRuns(out var lineWidth);

        _lines.Add(new TextLayoutLine(lineRuns, lineWidth, _input.LineHeight));

        _currentLine.Clear();
        _currentWidth = 0f;
    }

    private List<TextLayoutRun> BuildLineRuns(out float lineWidth)
    {
        var lineRuns = new List<TextLayoutRun>(_currentLine.Count);
        lineWidth = 0f;

        foreach (var buffer in _currentLine)
        {
            if (buffer.Text.Length == 0)
            {
                continue;
            }

            var text = buffer.Text.ToString();
            var width = _measurer.MeasureWidth(buffer.Source.Font, buffer.Source.FontSizePt, text);
            var (ascent, descent) = _measurer.GetMetrics(buffer.Source.Font, buffer.Source.FontSizePt);

            lineRuns.Add(new TextLayoutRun(
                buffer.Source.Source,
                text,
                buffer.Source.Font,
                buffer.Source.FontSizePt,
                width,
                ascent,
                descent,
                buffer.Source.Style.Color));

            lineWidth += width;
        }

        return lineRuns;
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
        if (Fits(_currentWidth + tokenWidth, _availableWidth))
        {
            AppendToken(run, token, tokenWidth);
            return true;
        }

        return false;
    }

    private void AppendToken(TextRunInput run, string token, float tokenWidth)
    {
        var buffer = _currentLine.Count > 0 ? _currentLine[^1] : null;
        if (buffer is not null && buffer.Source.RunId == run.RunId)
        {
            buffer.Append(token);
        }
        else
        {
            buffer = new LineRunBuffer(run);
            buffer.Append(token);
            _currentLine.Add(buffer);
        }

        _currentWidth += tokenWidth;
    }

    private float MeasureWidth(TextRunInput run, string text)
    {
        return _measurer.MeasureWidth(run.Font, run.FontSizePt, text);
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

    private static void TrimLineEnd(List<LineRunBuffer> runs)
    {
        for (var i = runs.Count - 1; i >= 0; i--)
        {
            var buffer = runs[i];
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

    private readonly struct TokenProcessor(TextLayoutLineBuilder builder, TextRunInput run, string token)
    {
        private readonly TextLayoutLineBuilder _builder = builder;
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

        private bool IsLeadingWhitespace()
        {
            return string.IsNullOrWhiteSpace(_token) && _builder._currentLine.Count == 0;
        }
    }

    private sealed class LineRunBuffer(TextRunInput source)
    {
        public TextRunInput Source { get; } = source;
        public StringBuilder Text { get; } = new();

        public void Append(string text)
        {
            Text.Append(text);
        }
    }
}
