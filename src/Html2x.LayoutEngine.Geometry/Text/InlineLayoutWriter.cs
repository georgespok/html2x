using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Orchestrates inline line layout assembly by delegating alignment, justification, and placement concerns.
/// </summary>
internal sealed class InlineLayoutWriter
{
    private readonly InlineAlignmentRules _alignmentRules;
    private readonly InlineJustificationRules _justificationRules;
    private readonly InlineLineBoundsRules _lineBoundsRules;
    private readonly TextRunLayout _textRunLayout;

    public InlineLayoutWriter(ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(measurer);

        _alignmentRules = new();
        _justificationRules = new(measurer, _alignmentRules);
        _lineBoundsRules = new();
        _textRunLayout = new(
            new(WriteSegment, new()),
            _lineBoundsRules);
    }

    public InlineFlowSegmentLayout WriteSegment(
        BlockBox blockContext,
        TextLayoutResult layout,
        float contentLeft,
        float contentTop,
        float contentWidth,
        string? textAlign)
    {
        ArgumentNullException.ThrowIfNull(blockContext);
        ArgumentNullException.ThrowIfNull(layout);

        var lines = new List<InlineLineLayout>(layout.Lines.Count);
        var nextTopY = contentTop;

        for (var lineIndex = 0; lineIndex < layout.Lines.Count; lineIndex++)
        {
            var line = layout.Lines[lineIndex];
            var topY = nextTopY;
            var baselineY = topY + GetBaselineAscent(line);
            var items = BuildLineItems(
                line,
                textAlign,
                contentWidth,
                contentLeft,
                topY,
                baselineY,
                lineIndex,
                layout.Lines.Count);
            var occupiedRect = _lineBoundsRules.CreateLineOccupiedRect(items, contentLeft, topY, line.LineHeight);
            var rect = _lineBoundsRules.CreateLineSlotRect(items, contentLeft, contentWidth, topY, line.LineHeight);

            lines.Add(new(
                lineIndex,
                rect,
                occupiedRect,
                baselineY,
                line.LineHeight,
                textAlign?.ToLowerInvariant(),
                items));

            nextTopY = topY + line.LineHeight;
        }

        return new(lines, contentTop, Math.Max(0f, nextTopY - contentTop));
    }

    private IReadOnlyList<InlineLineItemLayout> BuildLineItems(
        TextLayoutLine line,
        string? textAlign,
        float contentWidth,
        float contentLeft,
        float topY,
        float baselineY,
        int lineIndex,
        int lineCount)
    {
        var justificationPlan = _justificationRules.CreatePlan(
            textAlign,
            contentWidth,
            line.LineWidth,
            line,
            lineIndex,
            lineCount);

        return justificationPlan.ShouldJustify
            ? BuildJustifiedLineItems(line, contentLeft, topY, baselineY, justificationPlan)
            : BuildSequentialLineItems(line, textAlign, contentWidth, contentLeft, topY, baselineY, lineIndex,
                lineCount);
    }

    private IReadOnlyList<InlineLineItemLayout> BuildSequentialLineItems(
        TextLayoutLine line,
        string? textAlign,
        float contentWidth,
        float contentLeft,
        float topY,
        float baselineY,
        int lineIndex,
        int lineCount)
    {
        var lineOffsetX = _alignmentRules.ResolveLineOffset(
            textAlign,
            contentWidth,
            line.LineWidth,
            line,
            lineIndex,
            lineCount);
        var placement = new InlineLinePlacement(
            contentLeft,
            topY,
            line.LineHeight,
            baselineY,
            contentLeft + lineOffsetX);

        return _textRunLayout.Layout(line, placement, InlineJustificationRules.CreateSequentialTextPlacements);
    }

    private IReadOnlyList<InlineLineItemLayout> BuildJustifiedLineItems(
        TextLayoutLine line,
        float contentLeft,
        float topY,
        float baselineY,
        JustificationPlan justificationPlan)
    {
        var placement = new InlineLinePlacement(
            contentLeft,
            topY,
            line.LineHeight,
            baselineY,
            contentLeft);

        return _textRunLayout.Layout(
            line,
            placement,
            run => _justificationRules.CreateJustifiedTextPlacements(run, justificationPlan));
    }

    private static float GetBaselineAscent(TextLayoutLine line)
    {
        if (line.Runs.Count == 0)
        {
            return 0f;
        }

        var ascent = 0f;
        foreach (var run in line.Runs)
        {
            ascent = Math.Max(ascent, run.Ascent);
        }

        return ascent;
    }
}