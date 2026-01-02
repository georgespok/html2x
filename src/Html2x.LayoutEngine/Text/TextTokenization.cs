using System.Globalization;
using System.Text.RegularExpressions;

namespace Html2x.LayoutEngine.Text;

internal static class TextTokenization
{
    private static readonly Regex TokenRegex = new(@"\s+|\S+", RegexOptions.Multiline);

    public static IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var matches = TokenRegex.Matches(text);
        if (matches.Count == 0)
        {
            return [];
        }

        var tokens = new List<string>(matches.Count);
        foreach (Match match in matches)
        {
            if (match.Length > 0)
            {
                tokens.Add(match.Value);
            }
        }

        return tokens;
    }

    public static IEnumerable<string> SplitIntoLogicalLines(string text)
    {
        if (text is null)
        {
            yield break;
        }

        var lines = text.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            yield return line;
        }
    }

    public static IEnumerable<string> EnumerateGraphemes(string text)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            yield return enumerator.GetTextElement();
        }
    }
}
