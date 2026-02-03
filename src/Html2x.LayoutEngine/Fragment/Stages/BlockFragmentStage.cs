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
            CreateFragmentRecursive(block, null, state.Fragments, bindings, state.Observers, state);
        }

        return state.WithBlockBindings(bindings);
    }

    private static void CreateFragmentRecursive(BlockBox blockBox, BlockFragment? parentFragment, FragmentTree tree,
        ICollection<BlockFragmentBinding> bindings, IReadOnlyList<IFragmentBuildObserver> observers, FragmentBuildState state)
    {
        var fragment = BlockFragmentFactory.Create(blockBox, state);

        if (parentFragment is null)
        {
            tree.Blocks.Add(fragment);
        }
        else
        {
            parentFragment.Children.Add(fragment);
        }

        bindings.Add(new BlockFragmentBinding(blockBox, fragment));

        foreach (var observer in observers)
        {
            observer.OnBlockFragmentCreated(blockBox, fragment);
        }

        foreach (var child in blockBox.Children.OfType<BlockBox>())
        {
            CreateFragmentRecursive(child, fragment, tree, bindings, observers, state);
        }
    }
}
