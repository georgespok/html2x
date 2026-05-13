namespace Html2x.LayoutEngine.Geometry.InlineFlow;

/// <summary>
///     Resolves horizontal line offsets for supported text alignment modes.
/// </summary>
internal sealed class InlineAlignmentRules
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

        var align = textAlign?.ToLowerInvariant() ?? HtmlCssVocabulary.Defaults.TextAlign;
        var extra = Math.Max(0f, contentWidth - lineWidth);

        return align switch
        {
            HtmlCssVocabulary.CssValues.Center => extra / 2f,
            HtmlCssVocabulary.CssValues.Right => extra,
            HtmlCssVocabulary.CssValues.Justify when ShouldJustifyLine(line, lineIndex, lineCount) => 0f,
            _ => 0f
        };
    }

    public bool ShouldJustifyLine(TextLayoutLine line, int lineIndex, int lineCount) => lineIndex < lineCount - 1;
}