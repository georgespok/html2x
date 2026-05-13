using Html2x.LayoutEngine.Geometry.Construction;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

internal sealed class InlineRunCollector(
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

        var runBuffer = CreateRunBuffer(blockContext.Style, availableWidth);
        if (includeSyntheticListMarker)
        {
            TryAppendSyntheticListMarkerRun(blockContext, runBuffer);
        }

        var walker = new InlineRunTreeWalker(runBuffer);
        walker.CollectInlineFlow(inlineChildren);

        return runBuffer.Runs;
    }

    public IReadOnlyList<TextRunInput> CollectInlineBoxContent(
        BlockBox block,
        float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(block);

        var runBuffer = CreateRunBuffer(block.Style, availableWidth);
        var walker = new InlineRunTreeWalker(runBuffer);
        walker.CollectInlineBoxContent(block);

        return runBuffer.Runs;
    }

    private InlineRunBuffer CreateRunBuffer(ComputedStyle blockStyle, float availableWidth) =>
        new(
            blockStyle,
            availableWidth,
            _runConstruction,
            _textMeasurer,
            _lineHeightStrategy);

    private static void TryAppendSyntheticListMarkerRun(
        BlockBox blockContext,
        InlineRunBuffer runBuffer)
    {
        var marker = ListMarkerPolicy.CreateSyntheticMarker(blockContext);
        if (marker is not null)
        {
            runBuffer.TryAppendTextRun(marker);
        }
    }
}
