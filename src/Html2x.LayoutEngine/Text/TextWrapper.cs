using System.Text;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;

namespace Html2x.LayoutEngine.Text;

internal sealed class TextWrapper(ITextMeasurer textMeasurer)
{
    private readonly ITextMeasurer _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));

    public IReadOnlyList<string> Wrap(string text, FontKey font, float sizePt, float maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var lines = new List<string>();
        foreach (var rawLine in SplitIntoLogicalLines(text))
        {
            WrapSingleLine(rawLine, lines, font, sizePt, maxWidth);
        }

        return lines;
    }

    private bool Fits(string text, FontKey font, float sizePt, float maxWidth)
    {
        if (maxWidth <= 0f)
        {
            return false;
        }

        return _textMeasurer.MeasureWidth(font, sizePt, text) <= maxWidth;
    }

    private void WrapSingleLine(
        string rawLine,
        ICollection<string> destination,
        FontKey font,
        float sizePt,
        float maxWidth)
    {
        var current = new StringBuilder();

        if (rawLine.Length == 0)
        {
            destination.Add(string.Empty);
            return;
        }

        var tokens = TextTokenization.Tokenize(rawLine);
        foreach (var token in tokens)
        {
            var segment = token;
            if (string.IsNullOrWhiteSpace(segment) && current.Length == 0)
            {
                continue;
            }

            if (TryAppend(segment))
            {
                continue;
            }

            FlushLine();

            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            if (TryAppend(segment))
            {
                continue;
            }

            foreach (var element in TextTokenization.EnumerateGraphemes(segment))
            {
                if (TryAppend(element))
                {
                    continue;
                }

                FlushLine();
                current.Append(element);
            }
        }

        FlushLine(forceWhenEmpty: true);

        void FlushLine(bool forceWhenEmpty = false)
        {
            if (current.Length == 0)
            {
                if (forceWhenEmpty)
                {
                    destination.Add(string.Empty);
                }

                return;
            }

            var lineText = current.ToString().TrimEnd();
            destination.Add(lineText);
            current.Clear();
        }

        bool TryAppend(string segment)
        {
            var candidate = string.Concat(current.ToString(), segment);
            if (Fits(candidate, font, sizePt, maxWidth))
            {
                current.Append(segment);
                return true;
            }

            return false;
        }
    }

    private static IEnumerable<string> SplitIntoLogicalLines(string text)
    {
        foreach (var line in TextTokenization.SplitIntoLogicalLines(text))
        {
            yield return line;
        }
    }
}
