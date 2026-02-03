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

        switch (run.Kind)
        {
            case TextRunKind.LineBreak:
                FlushLine(forceWhenEmpty: true);
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
                FlushLine(forceWhenEmpty: true);
            }

            if (rawLine.Length == 0)
            {
                FlushLine(forceWhenEmpty: true);
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
                _lines.Add(new TextLayoutLine([], 0f, _input.LineHeight));
            }

            return;
        }

        TrimLineEnd(_currentLine);

        var lineRuns = BuildLineRuns(out var lineWidth);
        var lineHeight = ResolveLineHeight(lineRuns, _input.LineHeight);

        _lines.Add(new TextLayoutLine(lineRuns, lineWidth, lineHeight));

        _currentLine.Clear();
        _currentWidth = 0f;
    }

    private List<TextLayoutRun> BuildLineRuns(out float lineWidth)
    {
        var lineRuns = new List<TextLayoutRun>(_currentLine.Count);
        lineWidth = 0f;

        foreach (var buffer in _currentLine)
        {
            if (buffer.InlineObject is not null)
            {
                var inlineObject = buffer.InlineObject;
                var inlineAscent = inlineObject.Baseline;
                var inlineDescent = inlineObject.Height - inlineObject.Baseline;

                lineRuns.Add(new TextLayoutRun(
                    buffer.Source.Source,
                    string.Empty,
                    buffer.Source.Font,
                    buffer.Source.FontSizePt,
                    inlineObject.Width,
                    buffer.LeftSpacing,
                    buffer.RightSpacing,
                    inlineAscent,
                    inlineDescent,
                    buffer.Source.Style.Decorations,
                    buffer.Source.Style.Color,
                    inlineObject));

                lineWidth += buffer.LeftSpacing + inlineObject.Width + buffer.RightSpacing;
                continue;
            }

            if (buffer.Text.Length == 0)
            {
                continue;
            }

            var text = buffer.Text.ToString();
            var width = _measurer.MeasureWidth(buffer.Source.Font, buffer.Source.FontSizePt, text);
            var (ascent, descent) = _measurer.GetMetrics(buffer.Source.Font, buffer.Source.FontSizePt);

            var leftSpacing = buffer.LeftSpacing;
            var rightSpacing = buffer.RightSpacing;

            lineRuns.Add(new TextLayoutRun(
                buffer.Source.Source,
                text,
                buffer.Source.Font,
                buffer.Source.FontSizePt,
                width,
                leftSpacing,
                rightSpacing,
                ascent,
                descent,
                buffer.Source.Style.Decorations,
                buffer.Source.Style.Color));

            lineWidth += leftSpacing + width + rightSpacing;
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

        var tokenWidth = run.InlineObject.Width;
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
        if (_currentLine.Count > 0)
        {
            var buffer = _currentLine[^1];
            if (buffer.Source.RunId == run.RunId)
            {
                buffer.Append(token);
            }
            else
            {
                buffer = new LineRunBuffer(run);
                buffer.Append(token);
                _currentLine.Add(buffer);
                _currentWidth += buffer.LeftSpacing + buffer.RightSpacing;
            }
        }
        else
        {
            var buffer = new LineRunBuffer(run);
            buffer.Append(token);
            _currentLine.Add(buffer);
            _currentWidth += buffer.LeftSpacing + buffer.RightSpacing;
        }

        _currentWidth += tokenWidth;
    }

    private void AppendInlineObject(TextRunInput run, float tokenWidth)
    {
        var buffer = new LineRunBuffer(run, run.InlineObject);
        _currentLine.Add(buffer);
        _currentWidth += buffer.LeftSpacing + tokenWidth + buffer.RightSpacing;
    }

    private float MeasureWidth(TextRunInput run, string text)
    {
        return _measurer.MeasureWidth(run.Font, run.FontSizePt, text);
    }

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

    private static void TrimLineEnd(List<LineRunBuffer> runs)
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

    private sealed class LineRunBuffer
    {
        public TextRunInput Source { get; }
        public StringBuilder Text { get; } = new();
        public float LeftSpacing { get; }
        public float RightSpacing { get; }
        public InlineObjectLayout? InlineObject { get; }

        public LineRunBuffer(TextRunInput source, InlineObjectLayout? inlineObject = null)
        {
            Source = source;
            InlineObject = inlineObject;
            LeftSpacing = source.PaddingLeft + source.MarginLeft;
            RightSpacing = source.PaddingRight + source.MarginRight;
        }

        public void Append(string text)
        {
            Text.Append(text);
        }
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
