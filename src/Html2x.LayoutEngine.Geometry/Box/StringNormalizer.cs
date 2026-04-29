using System.Text;

namespace Html2x.LayoutEngine.Box;

internal static class StringNormalizer
{
    public static string NormalizeWhiteSpaceNormal(string text, TextNormalizationState state)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                state.PendingSpace = true;
                continue;
            }

            if (!state.AtLineStart && state.PendingSpace &&
                (state.HasWrittenContent || builder.Length > 0))
            {
                builder.Append(' ');
            }

            builder.Append(ch);
            state.PendingSpace = false;
            state.AtLineStart = false;
            state.HasWrittenContent = true;
        }

        return builder.ToString();
    }
}
