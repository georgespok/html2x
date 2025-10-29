using Html2x.Layout.Box;

namespace Html2x.Layout.Fragment;

public sealed class FragmentBuildContext(BoxTree boxTree)
{
    public BoxTree BoxTree { get; } = boxTree;
    public FragmentTree Result { get; } = new();
    public Dictionary<DisplayNode, Core.Layout.Fragment> Map { get; } = new();
}