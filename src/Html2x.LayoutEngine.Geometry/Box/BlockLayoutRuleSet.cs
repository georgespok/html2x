namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Selects the internal layout rule for a supported block kind.
/// </summary>
internal sealed class BlockLayoutRuleSet
{
    private readonly IReadOnlyList<IBlockLayoutRule> _rules;

    public BlockLayoutRuleSet(IEnumerable<IBlockLayoutRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.ToArray();
        if (_rules.Count == 0)
        {
            throw new ArgumentException("At least one block layout rule is required.", nameof(rules));
        }
    }

    public BlockLayoutRuleResult Layout(BlockBox block, BlockLayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(block);

        foreach (var rule in _rules)
        {
            if (rule.CanLayout(block))
            {
                return rule.Layout(block, request);
            }
        }

        throw new NotSupportedException($"No block layout rule handles '{block.GetType().Name}'.");
    }
}