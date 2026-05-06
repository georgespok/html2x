namespace Html2x.LayoutEngine.Geometry.Text;

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

        var align = textAlign?.ToLowerInvariant() ?? HtmlCssConstants.Defaults.TextAlign;
        var extra = Math.Max(0f, contentWidth - lineWidth);

        return align switch
        {
            HtmlCssConstants.CssValues.Center => extra / 2f,
            HtmlCssConstants.CssValues.Right => extra,
            HtmlCssConstants.CssValues.Justify when ShouldJustifyLine(line, lineIndex, lineCount) => 0f,
            _ => 0f
        };
    }

    public bool ShouldJustifyLine(TextLayoutLine line, int lineIndex, int lineCount) => lineIndex < lineCount - 1;
}