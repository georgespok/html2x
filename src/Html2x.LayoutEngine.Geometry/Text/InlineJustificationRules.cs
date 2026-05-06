using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Computes justification spacing and converts justified text runs into measured token placements.
/// </summary>
internal sealed class InlineJustificationRules(ITextMeasurer measurer, InlineAlignmentRules alignmentRules)
{
    private readonly InlineAlignmentRules _alignmentRules =
        alignmentRules ?? throw new ArgumentNullException(nameof(alignmentRules));

    private readonly ITextMeasurer _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));

    public JustificationPlan CreatePlan(
        string? textAlign,
        float contentWidth,
        float lineWidth,
        TextLayoutLine line,
        int lineIndex,
        int lineCount)
    {
        if (!float.IsFinite(contentWidth) || contentWidth <= 0f)
        {
            return JustificationPlan.None;
        }

        var align = textAlign?.ToLowerInvariant() ?? HtmlCssConstants.Defaults.TextAlign;
        if (!string.Equals(align, HtmlCssConstants.CssValues.Justify, StringComparison.OrdinalIgnoreCase))
        {
            return JustificationPlan.None;
        }

        if (!_alignmentRules.ShouldJustifyLine(line, lineIndex, lineCount))
        {
            return JustificationPlan.None;
        }

        var whitespaceCount = CountWhitespace(line);
        var extraSpace = Math.Max(0f, contentWidth - lineWidth);
        if (extraSpace <= 0f || whitespaceCount == 0)
        {
            return JustificationPlan.None;
        }

        return new(
            true,
            extraSpace,
            extraSpace / whitespaceCount);
    }

    public static IReadOnlyList<TextRunPlacement> CreateSequentialTextPlacements(TextLayoutRun run) =>
    [
        new(
            run.Text,
            run.Width,
            run.LeftSpacing,
            run.RightSpacing,
            0f)
    ];

    public IReadOnlyList<TextRunPlacement> CreateJustifiedTextPlacements(
        TextLayoutRun run,
        JustificationPlan plan)
    {
        var tokens = TextTokenization.Tokenize(run.Text);
        if (tokens.Count == 0)
        {
            return [];
        }

        var placements = new List<TextRunPlacement>(tokens.Count);
        for (var tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
        {
            var token = tokens[tokenIndex];
            var isFirstToken = tokenIndex == 0;
            var isLastToken = tokenIndex == tokens.Count - 1;
            var leftSpacing = isFirstToken ? run.LeftSpacing : 0f;
            var rightSpacing = isLastToken ? run.RightSpacing : 0f;
            var tokenWidth = _measurer.MeasureWidth(run.Font, run.FontSizePt, token);
            var whitespaceCount = CountWhitespace(token);
            var tokenExtra = whitespaceCount > 0 ? whitespaceCount * plan.PerWhitespaceExtra : 0f;

            placements.Add(new(
                token,
                tokenWidth,
                leftSpacing,
                rightSpacing,
                tokenExtra));
        }

        return placements;
    }

    internal static int CountWhitespace(TextLayoutLine line)
    {
        var count = 0;
        foreach (var run in line.Runs)
        {
            count += CountWhitespace(run.Text);
        }

        return count;
    }

    internal static int CountWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var count = 0;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                count++;
            }
        }

        return count;
    }
}