namespace Html2x.Abstractions.Layout.Fragments;

// Container/block-level box (flow or table cells can reuse this)
public sealed class BlockFragment : Fragment
{
    public IList<Fragment> Children { get; init; } = [];
}