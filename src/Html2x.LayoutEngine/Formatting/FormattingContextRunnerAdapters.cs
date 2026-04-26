using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Formatting;

/// <summary>
/// Adapts existing inline layout engines to the internal formatting context runner contract.
/// </summary>
internal sealed class InlineFormattingContextRunnerAdapter(IInlineLayoutEngine inlineEngine)
    : IInlineFormattingContextRunner
{
    private readonly IInlineLayoutEngine _inlineEngine =
        inlineEngine ?? throw new ArgumentNullException(nameof(inlineEngine));

    public InlineLayoutResult LayoutInlineContent(BlockBox block, InlineLayoutRequest request)
    {
        return _inlineEngine.Layout(block, request);
    }

    public InlineLayoutResult MeasureInlineContent(BlockBox block, InlineLayoutRequest request)
    {
        return _inlineEngine.Measure(block, request);
    }
}

/// <summary>
/// Adapts existing table layout engines to the internal formatting context runner contract.
/// </summary>
internal sealed class TableFormattingContextRunnerAdapter(ITableLayoutEngine tableEngine)
    : ITableFormattingContextRunner
{
    private readonly ITableLayoutEngine _tableEngine =
        tableEngine ?? throw new ArgumentNullException(nameof(tableEngine));

    public TableLayoutResult LayoutTable(TableBox table, float availableWidth)
    {
        return _tableEngine.Layout(table, availableWidth);
    }
}
