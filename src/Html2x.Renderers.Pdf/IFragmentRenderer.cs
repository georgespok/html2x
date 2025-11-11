using Html2x.Core.Layout;

namespace Html2x.Pdf;

public interface IFragmentRenderer
{
    void RenderBlock(BlockFragment fragment, Action<Fragment, IFragmentRenderer> renderChild);

    void RenderLine(LineBoxFragment fragment);

    void RenderImage(ImageFragment fragment);

    void RenderRule(RuleFragment fragment);
}
