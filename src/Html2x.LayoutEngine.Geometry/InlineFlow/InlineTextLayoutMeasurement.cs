using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

/// <summary>
///     Measures wrapped inline text layout from collected inline run inputs.
/// </summary>
internal sealed class InlineTextLayoutMeasurement(
    InlineRunConstruction runConstruction,
    ITextMeasurer textMeasurer,
    IFontMetricsMeasurer metrics,
    LineHeightRules lineHeightRules)
{
    private readonly LineHeightRules _lineHeightRules =
        lineHeightRules ?? throw new ArgumentNullException(nameof(lineHeightRules));

    private readonly IFontMetricsMeasurer _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

    private readonly InlineRunConstruction _runConstruction =
        runConstruction ?? throw new ArgumentNullException(nameof(runConstruction));

    private readonly TextLineLayout _textLayout = new(textMeasurer);

    private readonly ITextMeasurer _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));

    public TextLayoutResult? MeasureInlineFlow(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        float availableWidth,
        bool includeSyntheticListMarker)
    {
        var runs = CollectInlineFlow(blockContext, inlineChildren, availableWidth, includeSyntheticListMarker);
        return runs.Count == 0
            ? null
            : LayoutRuns(blockContext, runs, availableWidth);
    }

    public TextLayoutResult MeasureInlineBoxContent(BlockBox block, float availableWidth)
    {
        var runs = CollectInlineBoxContent(block, availableWidth);
        return LayoutRuns(block, runs, availableWidth);
    }

    public float ResolveLineHeight(BlockBox blockContext)
    {
        var font = _metrics.GetFontKey(blockContext.Style);
        var fontSize = _metrics.GetFontSize(blockContext.Style);
        var fontMeasurement = _textMeasurer.Measure(font, fontSize, string.Empty);
        var metrics = (fontMeasurement.Ascent, fontMeasurement.Descent);
        return _lineHeightRules.GetLineHeight(blockContext.Style, font, fontSize, metrics);
    }

    private TextLayoutResult LayoutRuns(
        BlockBox blockContext,
        IReadOnlyList<TextRunInput> runs,
        float availableWidth) =>
        _textLayout.Layout(new(runs, availableWidth, ResolveLineHeight(blockContext)));

    private IReadOnlyList<TextRunInput> CollectInlineFlow(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        float availableWidth,
        bool includeSyntheticListMarker)
    {
        var collection = CreateRunCollector();
        return collection.CollectInlineFlow(
            blockContext,
            inlineChildren,
            availableWidth,
            includeSyntheticListMarker);
    }

    private IReadOnlyList<TextRunInput> CollectInlineBoxContent(BlockBox block, float availableWidth)
    {
        var collection = CreateRunCollector();
        return collection.CollectInlineBoxContent(block, availableWidth);
    }

    private InlineRunCollector CreateRunCollector() =>
        new(
            _runConstruction,
            _textMeasurer,
            _lineHeightRules);
}
