using Html2x.LayoutEngine.Contracts.Style;
using Html2x.Text;

namespace Html2x.LayoutEngine.Text;

/// <summary>
/// Orchestrates inline line layout assembly by delegating alignment, justification, and placement concerns.
/// </summary>
internal sealed class InlineLayoutResultBuilder
{
    private readonly InlineAlignmentResolver _alignmentResolver;
    private readonly InlineJustificationPlanner _justificationPlanner;
    private readonly TextRunPlacementBuilder _placementBuilder;
    private readonly InlineLineBoundsCalculator _boundsCalculator;

    public InlineLayoutResultBuilder(ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(measurer);

        _alignmentResolver = new InlineAlignmentResolver();
        _justificationPlanner = new InlineJustificationPlanner(measurer, _alignmentResolver);
        _boundsCalculator = new InlineLineBoundsCalculator();
        _placementBuilder = new TextRunPlacementBuilder(
            new InlineObjectPlacementBuilder(BuildSegment),
            _boundsCalculator);
    }

    public InlineFlowSegmentLayout BuildSegment(
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
            var occupiedRect = _boundsCalculator.CreateLineOccupiedRect(items, contentLeft, topY, line.LineHeight);
            var rect = _boundsCalculator.CreateLineSlotRect(items, contentLeft, contentWidth, topY, line.LineHeight);

            lines.Add(new InlineLineLayout(
                lineIndex,
                rect,
                occupiedRect,
                baselineY,
                line.LineHeight,
                textAlign?.ToLowerInvariant(),
                items));

            nextTopY = topY + line.LineHeight;
        }

        return new InlineFlowSegmentLayout(lines, contentTop, Math.Max(0f, nextTopY - contentTop));
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
        var justificationPlan = _justificationPlanner.CreatePlan(
            textAlign,
            contentWidth,
            line.LineWidth,
            line,
            lineIndex,
            lineCount);

        return justificationPlan.ShouldJustify
            ? BuildJustifiedLineItems(line, contentLeft, topY, baselineY, justificationPlan)
            : BuildSequentialLineItems(line, textAlign, contentWidth, contentLeft, topY, baselineY, lineIndex, lineCount);
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
        var lineOffsetX = _alignmentResolver.ResolveLineOffset(
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

        return _placementBuilder.Build(line, placement, InlineJustificationPlanner.CreateSequentialTextPlacements);
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

        return _placementBuilder.Build(
            line,
            placement,
            run => _justificationPlanner.CreateJustifiedTextPlacements(run, justificationPlan));
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
