using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Carries placement inputs for laying out one block-level node in the current block flow.
/// </summary>
internal readonly record struct BlockLayoutRequest(
    float ContentX,
    float CursorY,
    float ContentWidth,
    float ParentContentTop,
    float PreviousBottomMargin,
    float CollapsedTopMargin);

/// <summary>
/// Defines a specialized handler for block-like nodes inside the current block formatting context.
/// </summary>
internal interface IBlockLayoutStrategy
{
    bool CanLayout(BlockBox node);

    BlockBox Layout(BlockLayoutEngine engine, BlockBox node, BlockLayoutRequest request);
}

/// <summary>
/// Selects the first registered block layout strategy that can handle a block-like node.
/// </summary>
internal sealed class BlockLayoutStrategyRegistry
{
    private readonly IReadOnlyList<IBlockLayoutStrategy> _strategies;

    public BlockLayoutStrategyRegistry(IEnumerable<IBlockLayoutStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = strategies.ToArray();
    }

    public static BlockLayoutStrategyRegistry CreateDefault()
    {
        return new BlockLayoutStrategyRegistry(CreateDefaultStrategies());
    }

    internal static IReadOnlyList<IBlockLayoutStrategy> CreateDefaultStrategies()
    {
        return
        [
            new ImageBlockLayoutStrategy(),
            new RuleBlockLayoutStrategy(),
            new StandardBlockLayoutStrategy()
        ];
    }

    public BlockBox Layout(BlockLayoutEngine engine, BlockBox node, BlockLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(node);

        var strategy = _strategies.FirstOrDefault(candidate => candidate.CanLayout(node));
        if (strategy is null)
        {
            throw new InvalidOperationException(
                $"No block layout strategy registered for '{node.GetType().Name}'.");
        }

        return strategy.Layout(engine, node, request);
    }

    private sealed class ImageBlockLayoutStrategy : IBlockLayoutStrategy
    {
        public bool CanLayout(BlockBox node) => node is ImageBox;

        public BlockBox Layout(BlockLayoutEngine engine, BlockBox node, BlockLayoutRequest request)
        {
            return engine.LayoutImageBlock((ImageBox)node, request);
        }
    }

    private sealed class RuleBlockLayoutStrategy : IBlockLayoutStrategy
    {
        public bool CanLayout(BlockBox node) => node is RuleBox;

        public BlockBox Layout(BlockLayoutEngine engine, BlockBox node, BlockLayoutRequest request)
        {
            return engine.LayoutRuleBlock((RuleBox)node, request);
        }
    }

    private sealed class StandardBlockLayoutStrategy : IBlockLayoutStrategy
    {
        public bool CanLayout(BlockBox node) => true;

        public BlockBox Layout(BlockLayoutEngine engine, BlockBox node, BlockLayoutRequest request)
        {
            return engine.LayoutStandardBlock(node, request);
        }
    }
}
