using System.Collections.Generic;
using Html2x.Abstractions.Layout;
using Html2x.LayoutEngine.Box;

namespace Html2x.LayoutEngine.Fragment;

public interface IFragmentBuildObserver
{
    void OnBlockFragmentCreated(BlockBox source, BlockFragment fragment);

    void OnInlineFragmentCreated(InlineBox source, BlockFragment parent, LineBoxFragment line);

    void OnSpecialFragmentCreated(DisplayNode source, Abstractions.Layout.Fragment fragment);

    void OnZOrderCompleted(IReadOnlyList<Abstractions.Layout.Fragment> fragments);
}
