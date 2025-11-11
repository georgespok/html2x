using Html2x.Abstractions.Layout;
using Html2x.LayoutEngine.Fragment;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Assertions;

internal sealed class FragmentTreeAssertion(FragmentTree tree)
{
    public FragmentTreeAssertion HasBlockCount(int expected)
    {
        tree.Blocks.Count.ShouldBe(expected);
        return this;
    }

    public BlockFragment GetBlock(int index) => tree.Blocks[index];
}

internal sealed class FragmentAssertion(Abstractions.Layout.Fragment fragment)
{
    public FragmentAssertion HasChildCount(int expected)
    {
        if (fragment is BlockFragment block)
        {
            block.Children.Count.ShouldBe(expected);
        }
        return this;
    }

    public T GetChild<T>(int index) where T : Abstractions.Layout.Fragment
    {
        if (fragment is not BlockFragment block)
        {
            throw new InvalidOperationException("Can only get children from BlockFragment");
        }
        return block.Children[index].ShouldBeOfType<T>();
    }
}