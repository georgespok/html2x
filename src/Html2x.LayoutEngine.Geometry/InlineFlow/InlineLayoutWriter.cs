using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

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
            var baselineY = topY + InlineBaselineRules.ResolveLineAscent(line);
            var request = new InlineLineLayoutRequest(
                line,
                textAlign,
                contentWidth,
                contentLeft,
                topY,
                baselineY,
                lineIndex,
                layout.Lines.Count);
            var items = BuildLineItems(request);
            var occupiedRect = _lineBoundsRules.CreateLineOccupiedRect(
                items,
                request.ContentLeft,
                request.TopY,
                request.Line.LineHeight);
            var rect = _lineBoundsRules.CreateLineSlotRect(
                items,
                request.ContentLeft,
                request.ContentWidth,
                request.TopY,
                request.Line.LineHeight);

            lines.Add(new(
                lineIndex,
                rect,
                occupiedRect,
                baselineY,
                request.Line.LineHeight,
                textAlign?.ToLowerInvariant(),
                items));

            nextTopY = topY + request.Line.LineHeight;
        }

        return new(lines, contentTop, Math.Max(0f, nextTopY - contentTop));
    }

    private IReadOnlyList<InlineLineItemLayout> BuildLineItems(InlineLineLayoutRequest request)
    {
        var justificationPlan = _justificationRules.CreatePlan(
            request.TextAlign,
            request.ContentWidth,
            request.Line.LineWidth,
            request.Line,
            request.LineIndex,
            request.LineCount);

        return justificationPlan.ShouldJustify
            ? BuildJustifiedLineItems(request, justificationPlan)
            : BuildSequentialLineItems(request);
    }

    private IReadOnlyList<InlineLineItemLayout> BuildSequentialLineItems(InlineLineLayoutRequest request)
    {
        var lineOffsetX = _alignmentRules.ResolveLineOffset(
            request.TextAlign,
            request.ContentWidth,
            request.Line.LineWidth,
            request.Line,
            request.LineIndex,
            request.LineCount);
        var placement = request.CreatePlacement(request.ContentLeft + lineOffsetX);

        return _textRunLayout.Layout(
            request.Line,
            placement,
            InlineJustificationRules.CreateSequentialTextPlacements);
    }

    private IReadOnlyList<InlineLineItemLayout> BuildJustifiedLineItems(
        InlineLineLayoutRequest request,
        JustificationPlan justificationPlan)
    {
        var placement = request.CreatePlacement(request.ContentLeft);

        return _textRunLayout.Layout(
            request.Line,
            placement,
            run => _justificationRules.CreateJustifiedTextPlacements(run, justificationPlan));
    }

    private readonly record struct InlineLineLayoutRequest(
        TextLayoutLine Line,
        string? TextAlign,
        float ContentWidth,
        float ContentLeft,
        float TopY,
        float BaselineY,
        int LineIndex,
        int LineCount)
    {
        public InlineLinePlacement CreatePlacement(float startX) =>
            new(
                ContentLeft,
                TopY,
                Line.LineHeight,
                BaselineY,
                startX);
    }
}
