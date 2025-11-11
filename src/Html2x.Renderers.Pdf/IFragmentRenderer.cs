using Html2x.Abstractions.Layout;

namespace Html2x.Renderers.Pdf;

public interface IFragmentRenderer
{
    void RenderBlock(BlockFragment fragment, Action<Fragment, IFragmentRenderer> renderChild);

    void RenderLine(LineBoxFragment fragment);

    void RenderImage(ImageFragment fragment);

    void RenderRule(RuleFragment fragment);
}
