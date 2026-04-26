using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

internal interface IInlineNodeMeasurer
{
    bool TryMeasure(
        DisplayNode node,
        InlineMeasurementContext context);
}

internal sealed class InlineMeasurementContext
{
    private readonly InlineRunCollector _collector;

    public InlineMeasurementContext(
        ComputedStyle blockStyle,
        float availableWidth,
        InlineRunFactory runFactory,
        ITextMeasurer textMeasurer,
        ILineHeightStrategy lineHeightStrategy)
    {
        BlockStyle = blockStyle ?? throw new ArgumentNullException(nameof(blockStyle));
        AvailableWidth = availableWidth;
        TextMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        LineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
        _collector = new InlineRunCollector(
            BlockStyle,
            AvailableWidth,
            runFactory ?? throw new ArgumentNullException(nameof(runFactory)),
            TextMeasurer,
            LineHeightStrategy);
    }

    public ComputedStyle BlockStyle { get; }

    public float AvailableWidth { get; }

    public ITextMeasurer TextMeasurer { get; }

    public ILineHeightStrategy LineHeightStrategy { get; }

    public IReadOnlyList<TextRunInput> Runs => _collector.Runs;

    public bool TryAppendInlineBlockRun(InlineBox inline) => _collector.TryAppendInlineBlockRun(inline);

    public bool TryAppendInlineBlockBoundaryRun(InlineBlockBoundaryBox boundary) =>
        _collector.TryAppendInlineBlockBoundaryRun(boundary);

    public bool TryAppendLineBreakRun(InlineBox inline) => _collector.TryAppendLineBreakRun(inline);

    public bool TryAppendTextRun(InlineBox inline) => _collector.TryAppendTextRun(inline);
}

internal sealed class InlineNodeMeasurerRegistry
{
    private readonly IReadOnlyList<IInlineNodeMeasurer> _measurers;

    public InlineNodeMeasurerRegistry(IEnumerable<IInlineNodeMeasurer> measurers)
    {
        ArgumentNullException.ThrowIfNull(measurers);
        _measurers = measurers.ToArray();
    }

    public static InlineNodeMeasurerRegistry CreateDefault()
    {
        return new InlineNodeMeasurerRegistry(CreateDefaultMeasurers());
    }

    internal static IReadOnlyList<IInlineNodeMeasurer> CreateDefaultMeasurers()
    {
        return
        [
            new InlineBlockBoundaryNodeMeasurer(),
            new InlineBlockNodeMeasurer(),
            new LineBreakNodeMeasurer(),
            new TextNodeMeasurer()
        ];
    }

    public bool TryMeasure(
        DisplayNode node,
        InlineMeasurementContext context)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var measurer in _measurers)
        {
            if (measurer.TryMeasure(node, context))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class InlineBlockBoundaryNodeMeasurer : IInlineNodeMeasurer
    {
        public bool TryMeasure(
            DisplayNode node,
            InlineMeasurementContext context)
        {
            return node is InlineBlockBoundaryBox boundary &&
                   context.TryAppendInlineBlockBoundaryRun(boundary);
        }
    }

    private sealed class InlineBlockNodeMeasurer : IInlineNodeMeasurer
    {
        public bool TryMeasure(
            DisplayNode node,
            InlineMeasurementContext context)
        {
            return node is InlineBox inline &&
                   context.TryAppendInlineBlockRun(inline);
        }
    }

    private sealed class LineBreakNodeMeasurer : IInlineNodeMeasurer
    {
        public bool TryMeasure(
            DisplayNode node,
            InlineMeasurementContext context)
        {
            return node is InlineBox inline &&
                   context.TryAppendLineBreakRun(inline);
        }
    }

    private sealed class TextNodeMeasurer : IInlineNodeMeasurer
    {
        public bool TryMeasure(
            DisplayNode node,
            InlineMeasurementContext context)
        {
            return node is InlineBox inline &&
                   context.TryAppendTextRun(inline);
        }
    }
}
