using Html2x.Abstractions.Layout.Text;

namespace Html2x.LayoutEngine.Text;

/// <summary>
/// Computes justification spacing and converts justified text runs into measured token placements.
/// </summary>
internal sealed class InlineJustificationPlanner
{
    private readonly ITextMeasurer _measurer;
    private readonly InlineAlignmentResolver _alignmentResolver;

    public InlineJustificationPlanner(ITextMeasurer measurer, InlineAlignmentResolver alignmentResolver)
    {
        _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
        _alignmentResolver = alignmentResolver ?? throw new ArgumentNullException(nameof(alignmentResolver));
    }

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
        if (!string.Equals(align, "justify", StringComparison.OrdinalIgnoreCase))
        {
            return JustificationPlan.None;
        }

        if (!_alignmentResolver.ShouldJustifyLine(line, lineIndex, lineCount))
        {
            return JustificationPlan.None;
        }

        var whitespaceCount = CountWhitespace(line);
        var extraSpace = Math.Max(0f, contentWidth - lineWidth);
        if (extraSpace <= 0f || whitespaceCount == 0)
        {
            return JustificationPlan.None;
        }

        return new JustificationPlan(
            ShouldJustify: true,
            ExtraSpace: extraSpace,
            PerWhitespaceExtra: extraSpace / whitespaceCount);
    }

    public static IReadOnlyList<TextRunPlacement> CreateSequentialTextPlacements(TextLayoutRun run)
    {
        return
        [
            new TextRunPlacement(
                run.Text,
                run.Width,
                run.LeftSpacing,
                run.RightSpacing,
                ExtraAfter: 0f)
        ];
    }

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

            placements.Add(new TextRunPlacement(
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

/// <summary>
/// Describes whether a line should be justified and how much extra space each whitespace receives.
/// </summary>
internal readonly record struct JustificationPlan(
    bool ShouldJustify,
    float ExtraSpace,
    float PerWhitespaceExtra)
{
    public static JustificationPlan None { get; } = new(false, 0f, 0f);
}
