using Html2x.LayoutEngine.Geometry.Box;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

internal sealed class InlineRunCollection(
    InlineRunConstruction runConstruction,
    ITextMeasurer textMeasurer,
    ILineHeightStrategy lineHeightStrategy)
{
    private readonly ILineHeightStrategy _lineHeightStrategy =
        lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));

    private readonly InlineRunConstruction _runConstruction =
        runConstruction ?? throw new ArgumentNullException(nameof(runConstruction));

    private readonly ITextMeasurer _textMeasurer =
        textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));

    public IReadOnlyList<TextRunInput> CollectInlineFlow(
        BlockBox blockContext,
        IReadOnlyList<BoxNode> inlineChildren,
        float availableWidth,
        bool includeSyntheticListMarker)
    {
        ArgumentNullException.ThrowIfNull(blockContext);
        ArgumentNullException.ThrowIfNull(inlineChildren);

        var collector = CreateCollector(blockContext.Style, availableWidth);
        if (includeSyntheticListMarker)
        {
            TryAppendSyntheticListMarkerRun(blockContext, collector);
        }

        var walker = new InlineRunTreeWalker(collector);
        walker.CollectInlineFlow(inlineChildren);

        return collector.Runs;
    }

    public IReadOnlyList<TextRunInput> CollectInlineBoxContent(
        BlockBox block,
        float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(block);

        var collector = CreateCollector(block.Style, availableWidth);
        var walker = new InlineRunTreeWalker(collector);
        walker.CollectInlineBoxContent(block);

        return collector.Runs;
    }

    private InlineRunCollector CreateCollector(ComputedStyle blockStyle, float availableWidth) =>
        new(
            blockStyle,
            availableWidth,
            _runConstruction,
            _textMeasurer,
            _lineHeightStrategy);

    private static void TryAppendSyntheticListMarkerRun(
        BlockBox blockContext,
        InlineRunCollector collector)
    {
        var marker = ListMarkerPolicy.CreateSyntheticMarker(blockContext);
        if (marker is not null)
        {
            collector.TryAppendTextRun(marker);
        }
    }
}
