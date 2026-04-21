using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Text;

internal interface IInlineNodeMeasurer
{
    bool TryMeasure(
        DisplayNode node,
        InlineMeasurementContext context,
        ICollection<TextRunInput> runs,
        ref int nextRunId);
}

internal sealed class InlineMeasurementContext
{
    private readonly InlineRunFactory _runFactory;

    public InlineMeasurementContext(
        ComputedStyle blockStyle,
        float availableWidth,
        InlineRunFactory runFactory,
        ITextMeasurer textMeasurer,
        ILineHeightStrategy lineHeightStrategy)
    {
        BlockStyle = blockStyle ?? throw new ArgumentNullException(nameof(blockStyle));
        AvailableWidth = availableWidth;
        _runFactory = runFactory ?? throw new ArgumentNullException(nameof(runFactory));
        TextMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        LineHeightStrategy = lineHeightStrategy ?? throw new ArgumentNullException(nameof(lineHeightStrategy));
    }

    public ComputedStyle BlockStyle { get; }

    public float AvailableWidth { get; }

    public ITextMeasurer TextMeasurer { get; }

    public ILineHeightStrategy LineHeightStrategy { get; }

    public bool TryAppendInlineBlockRun(InlineBox inline, ICollection<TextRunInput> runs, ref int nextRunId)
    {
        ArgumentNullException.ThrowIfNull(inline);
        ArgumentNullException.ThrowIfNull(runs);

        if (!_runFactory.TryBuildInlineBlockLayout(
                inline,
                AvailableWidth,
                TextMeasurer,
                LineHeightStrategy,
                out var inlineLayout) ||
            !_runFactory.TryBuildInlineBlockRun(inline, nextRunId, inlineLayout, out var inlineRun))
        {
            return false;
        }

        runs.Add(inlineRun);
        nextRunId++;
        return true;
    }

    public bool TryAppendInlineBlockBoundaryRun(
        InlineBlockBoundaryBox boundary,
        ICollection<TextRunInput> runs,
        ref int nextRunId)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        return TryAppendInlineBlockRun(boundary.SourceInline, runs, ref nextRunId);
    }

    public bool TryAppendLineBreakRun(InlineBox inline, ICollection<TextRunInput> runs, ref int nextRunId)
    {
        ArgumentNullException.ThrowIfNull(inline);
        ArgumentNullException.ThrowIfNull(runs);

        if (!_runFactory.TryBuildLineBreakRunFromBlockContext(inline, BlockStyle, nextRunId, out var lineBreakRun))
        {
            return false;
        }

        runs.Add(lineBreakRun);
        nextRunId++;
        return true;
    }

    public bool TryAppendTextRun(InlineBox inline, ICollection<TextRunInput> runs, ref int nextRunId)
    {
        ArgumentNullException.ThrowIfNull(inline);
        ArgumentNullException.ThrowIfNull(runs);

        if (!_runFactory.TryBuildTextRun(inline, nextRunId, out var textRun))
        {
            return false;
        }

        runs.Add(textRun);
        nextRunId++;
        return true;
    }
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
        InlineMeasurementContext context,
        ICollection<TextRunInput> runs,
        ref int nextRunId)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(runs);

        foreach (var measurer in _measurers)
        {
            if (measurer.TryMeasure(node, context, runs, ref nextRunId))
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
            InlineMeasurementContext context,
            ICollection<TextRunInput> runs,
            ref int nextRunId)
        {
            return node is InlineBlockBoundaryBox boundary &&
                   context.TryAppendInlineBlockBoundaryRun(boundary, runs, ref nextRunId);
        }
    }

    private sealed class InlineBlockNodeMeasurer : IInlineNodeMeasurer
    {
        public bool TryMeasure(
            DisplayNode node,
            InlineMeasurementContext context,
            ICollection<TextRunInput> runs,
            ref int nextRunId)
        {
            return node is InlineBox inline &&
                   context.TryAppendInlineBlockRun(inline, runs, ref nextRunId);
        }
    }

    private sealed class LineBreakNodeMeasurer : IInlineNodeMeasurer
    {
        public bool TryMeasure(
            DisplayNode node,
            InlineMeasurementContext context,
            ICollection<TextRunInput> runs,
            ref int nextRunId)
        {
            return node is InlineBox inline &&
                   context.TryAppendLineBreakRun(inline, runs, ref nextRunId);
        }
    }

    private sealed class TextNodeMeasurer : IInlineNodeMeasurer
    {
        public bool TryMeasure(
            DisplayNode node,
            InlineMeasurementContext context,
            ICollection<TextRunInput> runs,
            ref int nextRunId)
        {
            return node is InlineBox inline &&
                   context.TryAppendTextRun(inline, runs, ref nextRunId);
        }
    }
}
