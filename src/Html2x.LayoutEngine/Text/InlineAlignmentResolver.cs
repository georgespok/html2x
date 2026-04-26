namespace Html2x.LayoutEngine.Text;

/// <summary>
/// Resolves horizontal line offsets for supported text alignment modes.
/// </summary>
internal sealed class InlineAlignmentResolver
{
    public float ResolveLineOffset(
        string? textAlign,
        float contentWidth,
        float lineWidth,
        TextLayoutLine line,
        int lineIndex,
        int lineCount)
    {
        if (!float.IsFinite(contentWidth) || contentWidth <= 0f)
        {
            return 0f;
        }

        var align = textAlign?.ToLowerInvariant() ?? HtmlCssConstants.Defaults.TextAlign;
        var extra = Math.Max(0f, contentWidth - lineWidth);

        return align switch
        {
            "center" => extra / 2f,
            "right" => extra,
            "justify" when ShouldJustifyLine(line, lineIndex, lineCount) => 0f,
            _ => 0f
        };
    }

    public bool ShouldJustifyLine(TextLayoutLine line, int lineIndex, int lineCount)
    {
        return lineIndex < lineCount - 1;
    }
}
