namespace Html2x.Core.Layout;

// Container/block-level box (flow or table cells can reuse this)
public sealed class BlockFragment : Fragment
{
    public IReadOnlyList<Fragment> Children { get; init; } = [];
}