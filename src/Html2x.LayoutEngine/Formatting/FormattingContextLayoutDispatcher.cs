using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Formatting;

/// <summary>
/// Routes block-level nodes to the current formatting context owner without adding new modes.
/// </summary>
internal sealed class FormattingContextLayoutDispatcher
{
    private readonly BlockLayoutStrategyRegistry _blockStrategies;

    public FormattingContextLayoutDispatcher(BlockLayoutStrategyRegistry blockStrategies)
    {
        _blockStrategies = blockStrategies ?? throw new ArgumentNullException(nameof(blockStrategies));
    }

    public BlockBox Layout(BlockLayoutEngine engine, BlockBox node, BlockLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(node);

        var boundary = FormattingContextBoundaryResolver.Resolve(node, nameof(BlockLayoutEngine));
        return boundary.Role == FormattingContextRole.Table
            ? engine.LayoutTableBlock((TableBox)node, request)
            : _blockStrategies.Layout(engine, node, request);
    }
}
