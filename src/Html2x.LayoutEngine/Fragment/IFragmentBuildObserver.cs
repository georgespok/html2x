using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

public interface IFragmentBuildObserver
{
    void OnBlockFragmentCreated(BlockBox source, BlockFragment fragment);

    void OnInlineFragmentCreated(InlineBox source, BlockFragment parent, LineBoxFragment line);

    void OnSpecialFragmentCreated(DisplayNode source, Abstractions.Layout.Fragments.Fragment fragment);

    void OnZOrderCompleted(IReadOnlyList<Abstractions.Layout.Fragments.Fragment> fragments);
}
