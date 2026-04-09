using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment.Stages;

public sealed class BlockFragmentStage : IFragmentBuildStage
{
    public FragmentBuildState Execute(FragmentBuildState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var bindings = new List<BlockFragmentBinding>();

        foreach (var block in state.Boxes.Blocks)
        {
            var fragment = CreateFragmentRecursive(block, bindings, state.Observers, state);
            state.Fragments.Blocks.Add(fragment);
        }

        return state.WithBlockBindings(bindings);
    }

    private static BlockFragment CreateFragmentRecursive(
        BlockBox blockBox,
        ICollection<BlockFragmentBinding> bindings,
        IReadOnlyList<IFragmentBuildObserver> observers,
        FragmentBuildState state)
    {
        var fragment = BlockFragmentFactory.Create(blockBox, state);

        bindings.Add(new BlockFragmentBinding(blockBox, fragment));

        foreach (var observer in observers)
        {
            observer.OnBlockFragmentCreated(blockBox, fragment);
        }

        foreach (var child in blockBox.Children.OfType<BlockBox>())
        {
            _ = CreateFragmentRecursive(child, bindings, observers, state);
        }

        return fragment;
    }
}
